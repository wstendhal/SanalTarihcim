(() => {
    const tarihOlaylari = [
        "1453: Fatih Sultan Mehmet, İstanbul'un fethi için büyük topları döktürmeye başladı. Çağ kapatan hazırlıklar!",
        "1915: Çanakkale Deniz Zaferi; İtilaf Devletleri donanması boğazın serin sularına gömüldü.",
        "1920: İstanbul, İtilaf Devletleri tarafından resmen işgal edildi; ancak milli irade asla teslim olmadı.",
        "1923: Mustafa Kemal Atatürk, yeni Türkiye'nin ekonomik temelini İzmir İktisat Kongresi'nde attı.",
        "1876: İlk Türk Anayasası olan Kanun-i Esasi ilan edilerek parlamenter sisteme ilk adım atıldı.",
        "1071: Malazgirt Zaferi ile Anadolu'nun kapıları Türklere sonsuza dek açıldı."
    ];

    const overlay = document.querySelector('.lantern-overlay');

    function gununOlayiniGetir() {
        const today = new Date();
        const index = (today.getDate() + today.getMonth()) % tarihOlaylari.length;
        const textElem = document.getElementById('history-text');
        if (textElem) textElem.innerText = tarihOlaylari[index];
    }

    function sayacGuncelle() {
        const sepet = JSON.parse(localStorage.getItem('tarihSepeti')) || [];
        const sayac = document.getElementById('sepet-sayac');
        if (sayac) sayac.innerText = sepet.length;
    }

    function sepeteEkle(isim, gorsel) {
        const sepet = JSON.parse(localStorage.getItem('tarihSepeti')) || [];
        if (sepet.some(item => item.ad === isim)) {
            alert("Bu hazine zaten sepetinde var babuş!");
            return;
        }

        sepet.push({ ad: isim, resim: gorsel });
        localStorage.setItem('tarihSepeti', JSON.stringify(sepet));
        sayacGuncelle();
        alert(isim + " başarıyla sepete eklendi!");
    }

    document.addEventListener('mousemove', event => {
        if (!overlay) return;

        overlay.style.setProperty('--x', event.clientX + 'px');
        overlay.style.setProperty('--y', event.clientY + 'px');
    });

    document.addEventListener('click', event => {
        const target = event.target;
        if (!(target instanceof Element)) return;

        const action = target.closest('[data-action]');
        if (!action) return;

        if (action.dataset.action === 'close-history') {
            document.getElementById('history-card').style.display = 'none';
        } else if (action.dataset.action === 'toggle-night-mode') {
            document.body.classList.toggle('night-mode-active');
            const icon = action.querySelector('i');
            if (icon) {
                icon.className = document.body.classList.contains('night-mode-active')
                    ? 'fas fa-moon'
                    : 'fas fa-lightbulb';
            }
        } else if (action.dataset.action === 'add-to-cart') {
            sepeteEkle(action.dataset.title, action.dataset.image);
        }
    });

    const contactForm = document.querySelector('[data-contact-form]');
    if (contactForm) {
        contactForm.addEventListener('submit', event => {
            event.preventDefault();
            alert('Mesajınız arşivlere mühürlendi!');
        });
    }

    sayacGuncelle();
    gununOlayiniGetir();
})();
