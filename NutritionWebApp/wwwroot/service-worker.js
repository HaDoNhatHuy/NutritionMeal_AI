const CACHE = 'dinhduong-v2'; // THAY ĐỔI VERSION CACHE ĐỂ BUỘC BROWSER CẬP NHẬT
const dynamicUrls = [
    '/',
    '/Home/Index',
    '/Settings/Index',
    '/Chat/Index',
    // Thêm các trang động khác nếu có
];

self.addEventListener('install', e => {
    // Chỉ cache các tài nguyên tĩnh như CSS, JS, libs
    // Loại bỏ các trang MVC (CSHTML) khỏi danh sách cache cứng
    e.waitUntil(caches.open(CACHE).then(cache => {
        return cache.addAll([
            // '/lib/bootstrap/dist/css/bootstrap.min.css', (Các tài nguyên tĩnh)
            // '/css/site.css',
            // ... thêm các file tĩnh cần thiết
        ]).catch(err => {
            console.warn("Lỗi cache các tài nguyên tĩnh:", err);
        });
    }));
});

self.addEventListener('fetch', e => {
    const url = new URL(e.request.url);

    // KIỂM TRA: Nếu request là các trang động (HTML), BỎ QUA CACHE và FETCH TỪ NETWORK
    if (e.request.destination === 'document' || dynamicUrls.some(path => url.pathname === path)) {
        e.respondWith(fetch(e.request).catch(() => caches.match(e.request)));
        return;
    }

    // Đối với các tài nguyên tĩnh khác, vẫn dùng cache-first
    e.respondWith(caches.match(e.request).then(res => res || fetch(e.request)));
});