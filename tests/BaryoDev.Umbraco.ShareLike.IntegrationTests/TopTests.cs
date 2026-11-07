using System.Reflection;
using BaryoDev.Umbraco.ShareLike.Api;
using BaryoDev.Umbraco.ShareLike.Likes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using Umbraco.Cms.Api.Management.Services.PermissionFilter;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Packaging;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

namespace BaryoDev.Umbraco.ShareLike.IntegrationTests;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class TopTests : UmbracoIntegrationTest
{
    // Answers CurrentUser with the user a test picked; everything else is default.
    public class SecurityProxy : DispatchProxy
    {
        public static IUser? User { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "get_CurrentUser")
            {
                return User;
            }

            var type = targetMethod!.ReturnType;
            return type.IsValueType && type != typeof(void) ? Activator.CreateInstance(type) : null;
        }
    }

    private sealed class FixedAccessor(IBackOfficeSecurity security) : IBackOfficeSecurityAccessor
    {
        public IBackOfficeSecurity? BackOfficeSecurity { get; } = security;
    }

    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IBackOfficeSecurityAccessor>(
            new FixedAccessor(DispatchProxy.Create<IBackOfficeSecurity, SecurityProxy>()));

        // The filter is internal to the management API; register it the way the package gets it in a site.
        var impl = typeof(IDocumentPermissionFilterService).Assembly
            .GetType("Umbraco.Cms.Api.Management.Services.PermissionFilter.DocumentPermissionFilterService")!;
        builder.Services.TryAddScoped(typeof(IDocumentPermissionFilterService), impl);
    }

    [SetUp]
    public async Task RunPackageMigration()
    {
        SecurityProxy.User = null;
        var executor = Services.GetRequiredService<global::Umbraco.Cms.Core.Migrations.IMigrationPlanExecutor>();
        await executor.ExecutePlanAsync(new ShareLikeMigrationPlan(), string.Empty);
    }

    private ILikeStore Store => new LikeStore(ScopeProvider);

    private (Guid Section, Guid[] Posts) CreateSection(string name, int posts)
    {
        var content = Services.GetRequiredService<IContentService>();
        var section = content.Create(name, Constants.System.Root, "section");
        content.Save(section);
        var keys = new Guid[posts];
        for (var i = 0; i < posts; i++)
        {
            var post = content.Create($"{name} post {i}", section.Id, "post");
            content.Save(post);
            keys[i] = post.Key;
        }

        return (section.Key, keys);
    }

    private void CreateTypes()
    {
        var shortString = Services.GetRequiredService<IShortStringHelper>();
        var types = Services.GetRequiredService<IContentTypeService>();
        var postType = new ContentType(shortString, Constants.System.Root) { Alias = "post", Name = "post" };
        Assert.That(types.CreateAsync(postType, Constants.Security.SuperUserKey).GetAwaiter().GetResult().Success, Is.True);
        var sectionType = new ContentType(shortString, Constants.System.Root)
        {
            Alias = "section",
            Name = "section",
            AllowedAsRoot = true,
            AllowedContentTypes = [new ContentTypeSort(postType.Key, 0, "post")],
        };
        Assert.That(types.CreateAsync(sectionType, Constants.Security.SuperUserKey).GetAwaiter().GetResult().Success, Is.True);
    }

    private void Like(Guid post, int times)
    {
        var store = Store;
        for (var i = 0; i < times; i++)
        {
            store.Increment(post);
        }
    }

    private async Task<IUser> CreateEditor(Action<UserUpdateModel> startNodes)
    {
        var groups = Services.GetRequiredService<IUserGroupService>();
        var group = new UserGroup(Services.GetRequiredService<IShortStringHelper>())
        {
            Alias = "testEditors",
            Name = "Test editors",
            Permissions = new HashSet<string> { "Umb.Document.Read" },
        };
        group.AddAllowedSection(Constants.Applications.Content);
        var madeGroup = await groups.CreateAsync(group, Constants.Security.SuperUserKey);
        Assert.That(madeGroup.Success, Is.True, "create group: " + madeGroup.Status);

        var users = Services.GetRequiredService<IUserService>();
        var created = await users.CreateAsync(Constants.Security.SuperUserKey, new UserCreateModel
        {
            Email = "editor@example.com",
            UserName = "editor@example.com",
            Name = "Editor",
            Kind = UserKind.Default,
            UserGroupKeys = new HashSet<Guid> { madeGroup.Result.Key },
        });
        Assert.That(created.Success, Is.True, "create user: " + created.Status);
        var key = created.Result.CreatedUser!.Key;

        var update = new UserUpdateModel
        {
            ExistingUserKey = key,
            Email = "editor@example.com",
            UserName = "editor@example.com",
            Name = "Editor",
            LanguageIsoCode = "en-US",
            UserGroupKeys = new HashSet<Guid> { madeGroup.Result.Key },
        };
        startNodes(update);
        var updated = await users.UpdateAsync(Constants.Security.SuperUserKey, update);
        Assert.That(updated.Success, Is.True, "update user: " + updated.Status);

        return (await users.GetAsync(key))!;
    }

    private async Task<List<MostLikedItem>> Top(IUser user, int take)
    {
        SecurityProxy.User = user;
        var controller = new ShareLikeController(
            Store,
            GetRequiredService<IBackOfficeSecurityAccessor>(),
            GetRequiredService<IEntityService>(),
            GetRequiredService<IDocumentPermissionFilterService>(),
            GetRequiredService<AppCaches>());

        var result = await controller.Top(take);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null, "expected 200, got " + result);
        return (List<MostLikedItem>)ok!.Value!;
    }

    // Section A holds the editor's start node and is liked least; section B holds the global top.
    private (Guid[] A, Guid[] B) Arrange(out Guid sectionA)
    {
        CreateTypes();
        var a = CreateSection("A", 4);
        var b = CreateSection("B", 6);
        sectionA = a.Section;
        for (var i = 0; i < a.Posts.Length; i++)
        {
            Like(a.Posts[i], 1 + i);
        }

        for (var i = 0; i < b.Posts.Length; i++)
        {
            Like(b.Posts[i], 10 + i);
        }

        return (a.Posts, b.Posts);
    }

    [Test]
    public async Task Top_returns_a_full_page_for_a_restricted_editor()
    {
        var (a, b) = Arrange(out var sectionA);
        var editor = await CreateEditor(u => u.ContentStartNodeKeys = new HashSet<Guid> { sectionA });

        var items = await Top(editor, 3);

        Assert.That(items, Has.Count.EqualTo(3));
        Assert.That(items.Select(i => i.Key), Is.EqualTo(new[] { a[3], a[2], a[1] }));
        Assert.That(items.Select(i => i.Count), Is.EqualTo(new[] { 4, 3, 2 }));
        Assert.That(items.Select(i => i.Key).Intersect(b), Is.Empty);
    }

    [Test]
    public async Task Root_user_sees_everything()
    {
        var (_, b) = Arrange(out _);
        var editor = await CreateEditor(u => u.HasContentRootAccess = true);

        var items = await Top(editor, 3);

        Assert.That(items.Select(i => i.Key), Is.EqualTo(new[] { b[5], b[4], b[3] }));
    }

    [Test]
    public async Task Root_access_plus_an_unrelated_start_node_still_sees_everything()
    {
        var (_, b) = Arrange(out var sectionA);
        var editor = await CreateEditor(u =>
        {
            u.HasContentRootAccess = true;
            u.ContentStartNodeKeys = new HashSet<Guid> { sectionA };
        });

        var items = await Top(editor, 3);

        Assert.That(items.Select(i => i.Key), Is.EqualTo(new[] { b[5], b[4], b[3] }));
    }

    [Test]
    public async Task User_without_start_nodes_sees_nothing()
    {
        Arrange(out _);
        var editor = await CreateEditor(_ => { });

        Assert.That(await Top(editor, 3), Is.Empty);
    }
}
