// Plain custom element with no build step, same approach as the other BaryoDev dashboards.
const API = "/umbraco/management/api/v1/share-like";
const BASE = "/App_Plugins/BaryoDev.ShareLike";

const Base = await import("@umbraco-cms/backoffice/element-api")
  .then(({ UmbElementMixin }) => UmbElementMixin(HTMLElement))
  .catch(() => HTMLElement);

function esc(value) {
  return String(value ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);
}

async function token(host) {
  try {
    const [{ UmbContextConsumer }, { UMB_AUTH_CONTEXT }] = await Promise.all([
      import("@umbraco-cms/backoffice/context-api"),
      import("@umbraco-cms/backoffice/auth"),
    ]);
    const consumer = new UmbContextConsumer(host, UMB_AUTH_CONTEXT);
    const auth = consumer.asPromise();
    consumer.request();
    return (await auth).getLatestToken();
  } catch {
    return null;
  }
}

class ShareLikeDashboard extends Base {
  async connectedCallback() {
    super.connectedCallback?.();
    this.innerHTML = `
      <link rel="stylesheet" href="${BASE}/brand.css">
      <div class="sl">
        <div class="sl-head"><img src="${BASE}/logo.svg" alt="baryo.dev"><span>Most liked</span></div>
        <div class="sl-box" id="sl-body"><p class="sl-note">Loading.</p></div>
      </div>`;
    await this.#load();
  }

  async #load() {
    const body = this.querySelector("#sl-body");
    try {
      const t = await token(this);
      const response = await fetch(`${API}/top?take=20`, {
        credentials: "same-origin",
        headers: t ? { Authorization: `Bearer ${t}` } : {},
      });
      if (response.status === 401 || response.status === 403) throw new Error("You do not have access to this.");
      if (!response.ok) throw new Error(`Request failed (${response.status}).`);
      const items = await response.json();
      if (!items.length) {
        body.innerHTML = `<h3>Most liked content</h3><p class="sl-note">No likes yet.</p>`;
        return;
      }
      const max = Math.max(...items.map((i) => i.count));
      const rows = items.map((i) => `
        <tr>
          <td>${esc(i.name)}<div class="sl-bar"><div style="width:${Math.round((i.count / max) * 100)}%"></div></div></td>
          <td class="n">${i.count}</td>
        </tr>`).join("");
      body.innerHTML = `
        <h3>Most liked content</h3>
        <table class="sl-table"><thead><tr><th>Content</th><th class="n">Likes</th></tr></thead><tbody>${rows}</tbody></table>`;
    } catch (e) {
      body.innerHTML = `<div class="sl-err">${esc(e.message)}</div>`;
    }
  }
}

customElements.define("baryodev-share-like-dashboard", ShareLikeDashboard);

export default ShareLikeDashboard;
export { ShareLikeDashboard as element };
