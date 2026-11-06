(function () {
  document.querySelectorAll('[data-bdsl-share]').forEach(function (box) {
    var url = box.getAttribute('data-url');
    var title = box.getAttribute('data-title');
    var nativeBtn = box.querySelector('[data-bdsl-native]');
    var copyBtn = box.querySelector('[data-bdsl-copy]');
    var canNative = !!(nativeBtn && navigator.share);
    if (canNative) {
      nativeBtn.hidden = false;
      nativeBtn.addEventListener('click', function () {
        navigator.share({ title: title, url: url }).catch(function () {});
      });
    }
    // Native share wins when the browser has it, as on baryo.dev.
    if (copyBtn && !canNative && navigator.clipboard) {
      copyBtn.hidden = false;
      copyBtn.addEventListener('click', function () {
        navigator.clipboard.writeText(url).then(function () { copyBtn.textContent = 'Copied'; }).catch(function () {});
      });
    }
  });
})();
