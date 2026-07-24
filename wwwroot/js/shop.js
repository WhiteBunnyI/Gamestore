window.Shop = window.Shop || (function () {

    const log = console.log;

    const game_card = `<div class="col">
            <div class="card h-50 shadow-sm flex-shrink-1">
                <img src="https://placehold.co/600x400/e9ecef/495057?text=Название" class="card-img-top" alt="Альт. название">
                <div class="card-body d-flex flex-column">
                    <span class="badge bg-success w-50 mb-2">значок</span>
                    <h5 class="card-title">Название карточки</h5>
                    <p class="card-text text-muted flex-grow-1">Описание</p>
                    <div class="mt-auto">
                        <div class="mb-3">
                            <span class="old-price">Старая цена</span>
                            <span class="price-tag">Новая цена</span>
                        </div>
                        <button class="btn btn-primary w-100"> В корзину</button>
                    </div>
                </div>
            </div>
        </div>`;

    let elements = null;
    let page = 1;

    let cards = null;
    let games = null;

    let sum_cart = 0;
    let game_cart = [];

    async function OnDomLoaded() {

        elements = {
            cardGrid: document.querySelector('#card-grid'),
            cartText: document.querySelector('#cart button'),
            cartContainer: document.querySelector('#cart ul'),
        }

        cards = [];
        for (let i = 0; i < 12; i++) {
            let child = document.createElement('div');
            child.innerHTML = game_card;
            HideElement(child);

            let el = {
                parent: child,
                icon: child.querySelector('.card-img-top'),
                badge: child.querySelector('.badge'),
                title: child.querySelector('.card-title'),
                desc: child.querySelector('.card-text'),
                oldPrice: child.querySelector('.old-price'),
                price: child.querySelector('.price-tag'),
                btn: child.querySelector('.btn'),
            }
            el.btn.addEventListener('click', (event) => OnBtnClick(event, el));
            cards.push(el);
            elements.cardGrid.appendChild(child);
        }

        window.Auth.onUpdateAuthUI.push(UpdateShopUI);
        window.Auth.onUpdateAuthUI.push(UpdateCartUI);
        window.Auth.onLogout.push(() => { game_cart = []; sum = 0; });

        UpdateShopUI();
        UpdateCartUI();
    }

    function UpdateCartUI() {
        if (game_cart.length === 0) {
            //Cart is empty
            elements.cartText.textContent = 'Корзина';
            elements.cartContainer.innerHTML = '<li class="mx-3">Корзина пуста</li>';
            return;
        }

        elements.cartContainer.innerHTML = '';
        sum_cart = 0;
        for (let i of game_cart) {
            const game = i;
            const li = document.createElement('li');
            li.classList.add('dropdown-item');
            li.textContent = `${game.title} (${game.price})`;
            li.addEventListener('click', () => {
                const index = games.indexOf(game);
                OnBtnClick(null, cards[index]);
            });
            elements.cartContainer.appendChild(li);
            sum_cart += game.price;
        }

        elements.cartText.textContent = `Корзина (Сумма: ${sum_cart.toFixed(2)} $)`;

        const separate = document.createElement('hr');
        separate.classList.add('dropdown-divider');

        const btn = document.createElement('button');
        btn.classList.add('badge', 'bg-primary', 'dropdown-item');
        btn.type = 'button';
        btn.textContent = 'Купить все';
        btn.addEventListener('click', OnBuyBtnPressed);

        elements.cartContainer.appendChild(separate);
        elements.cartContainer.appendChild(btn);
    }

    function UpdateShopUI() {
        if (cards === null || games === null) {
            return;
        }
        const maxDescLen = 35;
        for (let i = 0; i < games.length; i++) {
            //Exists
            let el = cards[i];
            ShowElement(el.parent);
            el.icon.src = `https://placehold.co/600x400/e9ecef/495057?text=${games[i].title}`;
            HideElement(el.badge);
            el.title.textContent = games[i].title;
            el.desc.textContent = games[i].description.substring(0, maxDescLen);
            if (el.desc.textContent.length === maxDescLen) {
                el.desc.textContent += '...';
            }
            HideElement(el.oldPrice);
            el.price.textContent = games[i].price + ' $';
            ShowElement(el.btn);
            UpdateCardBtnUI(el);
            if (!window.Auth.isAuth() || window.Auth.isAdmin()) {
                HideElement(el.btn);
            }
        }

        for (let i = games.length; i < 12; i++) {
            //Disable (Not exists)
            let el = cards[i];
            HideElement(el.parent);
        }
    }

    async function OnBuyBtnPressed(event) {
        await BuyGames();
    }

    async function BuyGames() {
        if (Auth.wallet() < sum_cart) {
            notify.error(`На балансе не хватает денег!`);
            return;
        }

        let game_ids = [];
        for (let i of game_cart) {
            game_ids.push(i.id);
        }

        if (game_ids.length === 0) {
            notify.error('В корзине нет игр!');
            return;
        }

        let options = {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(game_ids),
        }
        const response = await window.Auth.ExecuteUrlWithToken('api/games/buy', options);

        if (response.status === 200) {
            notify.info("Покупка успешно совершена!");
            Auth.UpdateAuthUI();
        }
    }

    function UpdateCardBtnUI(el) {
        const index = cards.indexOf(el);
        const game = games[index];
        const cart_id = game_cart.indexOf(game);
        const isAdded = cart_id !== -1;

        //Game was already added
        if (!isAdded) {
            el.btn.classList.remove('btn-success');
            el.btn.classList.add('btn-primary');
            el.btn.textContent = 'В корзину';
            return;
        }

        el.btn.classList.remove('btn-primary');
        el.btn.classList.add('btn-success');
        el.btn.textContent = 'Уже в корзине';
    }

    function OnBtnClick(event, card) {
        const index = cards.indexOf(card);
        const game = games[index];
        const cart_id = game_cart.indexOf(game);
        const isAdded = cart_id !== -1;

        if (isAdded) {
            game_cart.splice(cart_id, 1);
        }
        else {
            game_cart.push(game);
        }

        UpdateCardBtnUI(card);

        UpdateCartUI();
        log(`cart: ${game_cart}`);
    }

    async function GetGames() {
        const response = await fetch(`/api/games/get/${page}`);
        games = await response.json();

        return response;
    }

    function ShowElement(el) {
        el.classList.remove('d-none');
    }
    function HideElement(el) {
        el.classList.add('d-none');
    }

    window.document.addEventListener("DOMContentLoaded", OnDomLoaded);
    GetGames().then(UpdateShopUI);
})();
