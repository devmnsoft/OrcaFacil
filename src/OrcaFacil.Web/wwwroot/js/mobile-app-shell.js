/* mobile-app-shell.js — gives the installed PWA an app-like bottom tab bar.
   Only activates when the app is running standalone (installed / full-screen) AND the viewport is
   small: it then marks <body class="of-appstandalone">, which CSS uses to reveal the bottom bar and
   inset the content. In a normal browser (any size) or on desktop it stays dormant, so the existing
   sidebar/hamburger navigation and the PWA security band are untouched. */
(function () {
  'use strict';

  // Small-screen cutoff for the tabbed shell (phones/tablets in portrait).
  var MAX_WIDTH = 820;
  var SHELL_CLASS = 'of-appstandalone';

  function isStandalone() {
    try {
      if (window.matchMedia && (
        window.matchMedia('(display-mode: standalone)').matches ||
        window.matchMedia('(display-mode: minimal-ui)').matches
      )) return true;
    } catch (e) { /* very old engines without display-mode media */ }
    return window.navigator && window.navigator.standalone === true; // iOS Safari installed
  }

  function setActiveTab() {
    var path = window.location.pathname;
    var links = document.querySelectorAll('.of-app-tabbar a');
    links.forEach(function (link) {
      var prefix = link.getAttribute('data-path-prefix') || '';
      var active = prefix !== '' && path.indexOf(prefix) === 0;
      link.classList.toggle('is-active', active);
      if (active) link.setAttribute('aria-current', 'page'); else link.removeAttribute('aria-current');
    });
  }

  function sync() {
    var on = isStandalone() && window.innerWidth <= MAX_WIDTH;
    document.body.classList.toggle(SHELL_CLASS, on);
    if (on) setActiveTab();
  }

  function init() { sync(); }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
  window.addEventListener('resize', sync);
  window.addEventListener('orientationchange', sync);
})();
