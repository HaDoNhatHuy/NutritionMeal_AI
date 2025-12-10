// Mobile Navigation
document.addEventListener('DOMContentLoaded', function () {
    const menuToggle = document.getElementById('menuToggle');
    const sidebarClose = document.getElementById('sidebarClose');
    const mobileSidebar = document.getElementById('mobileSidebar');
    const mobileOverlay = document.getElementById('mobileOverlay');

    function openSidebar() {
        mobileSidebar.classList.add('active');
        mobileOverlay.classList.add('active');
        document.body.style.overflow = 'hidden';
    }

    function closeSidebar() {
        mobileSidebar.classList.remove('active');
        mobileOverlay.classList.remove('active');
        document.body.style.overflow = '';
    }

    if (menuToggle) {
        menuToggle.addEventListener('click', openSidebar);
    }

    if (sidebarClose) {
        sidebarClose.addEventListener('click', closeSidebar);
    }

    if (mobileOverlay) {
        mobileOverlay.addEventListener('click', closeSidebar);
    }

    // Active nav item
    const currentPath = window.location.pathname;
    document.querySelectorAll('.nav-item, .bottom-nav-item').forEach(item => {
        if (item.getAttribute('href') === currentPath) {
            item.classList.add('active');
        }
    });

    // Prevent zoom on input focus (iOS)
    document.querySelectorAll('input, select, textarea').forEach(el => {
        el.addEventListener('focus', function () {
            document.querySelector('meta[name=viewport]').setAttribute('content',
                'width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no');
        });

        el.addEventListener('blur', function () {
            document.querySelector('meta[name=viewport]').setAttribute('content',
                'width=device-width, initial-scale=1.0');
        });
    });
});

// Haptic Feedback (if supported)
function vibrateDevice(duration = 10) {
    if ('vibrate' in navigator) {
        navigator.vibrate(duration);
    }
}

// Add vibration to all buttons and links
document.addEventListener('click', function (e) {
    if (e.target.matches('button, a, .clickable')) {
        vibrateDevice();
    }
}, true);