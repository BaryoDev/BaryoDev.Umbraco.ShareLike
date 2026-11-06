(function () {
  document.querySelectorAll('[data-bdsl-like]').forEach(function (box) {
    var btn = box.querySelector('[data-bdsl-like-btn]');
    var num = box.querySelector('[data-bdsl-like-count]');
    var label = box.querySelector('[data-bdsl-like-label]');
    var status = box.querySelector('[data-bdsl-like-status]');
    // The page is the same for everyone, so the liked state comes from the cookie.
    if (document.cookie.split('; ').indexOf(box.getAttribute('data-cookie') + '=1') !== -1) {
      btn.disabled = true;
      btn.setAttribute('aria-pressed', 'true');
    }
    function show(n) {
      num.textContent = n;
      label.textContent = n === 1 ? 'like' : 'likes';
    }
    btn.addEventListener('click', function () {
      btn.disabled = true;
      fetch(box.getAttribute('data-url'), { method: 'POST', credentials: 'same-origin' })
        .then(function (r) {
          if (r.status === 429) { btn.disabled = false; status.textContent = 'Too many tries, wait a minute.'; return null; }
          return r.ok || r.status === 409 ? r.json() : null;
        })
        .then(function (d) {
          if (!d) { if (btn.disabled) btn.disabled = false; return; }
          show(d.count);
          btn.setAttribute('aria-pressed', 'true');
          status.textContent = 'Thanks.';
        })
        .catch(function () { btn.disabled = false; });
    });
  });
})();
