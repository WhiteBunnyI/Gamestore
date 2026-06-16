// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

window.Main = window.Main || (function () {

    let log = console.log;

    let access_token = null;
    let user_info = null;
    let status = null;

    let is_registering = false;
    let elements = null;

    const refresh_cookie_name = 'refresh_token';
    const access_cookie_name = 'access_token';

    let onUpdateAuthUI = [];
    let onLogout = [];

    async function OnDomLoaded() {
        const loginStatus = CheckLoginStatus();

        let profile = {
            offcanvas: document.querySelector('#profileOffcanvas'),
            icon: document.querySelector('#profile-icon'),
            badge: document.querySelector('.offcanvas .badge'),
            wallet: document.querySelector('#wallet'),
            login: document.querySelector('#profile-login'),
            profile_btn: document.querySelector('#profile-profile'),
            lib_btn: document.querySelector('#profile-lib'),
            favorite_btn: document.querySelector('#profile-favorite'),
            settings_btn: document.querySelector('#profile-settings'),
            logout_btn: document.querySelector('#logout-btn'),
        }

        profile.logout_btn.addEventListener('click', OnLogoutBtnPressed);

        let login_form = {
            modal: document.querySelector('#authModal'),
            form: document.querySelector('#loginForm'),
            submit_btn: document.querySelector('.modal .modal-body button'),
            login_btn: document.querySelector('#guest-buttons .btn-outline-light'),
            register_btn: document.querySelector('#guest-buttons .btn-primary'),
            change_btn: document.querySelector('#change-form'),
            login: document.querySelector('#loginInput'),
            password: document.querySelector('#passwordInput'),
            footer: document.querySelector('div .modal-body p'),
            header: document.querySelector('#authModalLabel'),
        }
        
        login_form.form.addEventListener('submit', OnLoginFormSubmit);
        login_form.login_btn.addEventListener('click', OnLoginBtnPressed);
        login_form.register_btn.addEventListener('click', OnRegisterBtnPressed);
        login_form.change_btn.addEventListener('click', OnChangeFormBtnPressed);

        elements = {
            auth_buttons: document.querySelector('#guest-buttons'),
            profile_button: document.querySelector('#user-button'),
            login_form: login_form,
            profile: profile,
        }

        await loginStatus;
        UpdateAuthUI();
    }

    function UpdateAuthUI() {
        if (status !== null && status.isAuth) {
            Logged();

            elements.profile.login.textContent = status.login;
            let badgeText = `Активен`;
            if (status.isAdmin) {
                badgeText += ` (Admin)`;
            }
            elements.profile.badge.textContent = badgeText;
            elements.profile.wallet.textContent = `Кошелек: ${user_info.wallet} $`;
            for (let i of onUpdateAuthUI) {
                i();
            }

            return;
        }

        for (let i of onUpdateAuthUI) {
            i();
        }
        NotLogged();
    }

    function UpdateLoginFormUI() {
        if (elements === null) {
            //Dom not loaded
            return;
        }

        if (is_registering) {
            elements.login_form.header.textContent = 'Регистрация аккаунта';
            elements.login_form.submit_btn.textContent = 'Зарегистрировать';
            elements.login_form.footer.textContent = 'Есть аккаунт?';
            elements.login_form.change_btn.textContent = 'Войти';
            return;
        }
        elements.login_form.header.textContent = 'Вход в аккаунт';
        elements.login_form.submit_btn.textContent = 'Войти';
        elements.login_form.footer.textContent = 'Нет аккаунта?';
        elements.login_form.change_btn.textContent = 'Зарегистрироваться';
    }

    async function OnLoginFormSubmit(event) {
        event.preventDefault();
        const loginData = elements.login_form.login.value;
        const password = elements.login_form.password.value;

        const data = {
            login: loginData,
            password: password,
        };

        const func = is_registering ? Register : Login;
        await func(data);

        if (access_token !== null) {
            if (document.activeElement) {
                document.activeElement.blur(); // Убирает фокус с текущего элемента
            }
            const modalInstance = bootstrap.Modal.getInstance(elements.login_form.modal)
            modalInstance.hide(); //show()
            await Status();
            await GetInfo();
            UpdateAuthUI();
            return;
        }
    }

    async function Login(data) {
        log(data);
        const response = await fetch(`/api/auth/login`, {
            method: 'POST',
            credentials: 'same-origin', // Если используете HttpOnly куки для аутентификации то include
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });

        if (response.status === 200) {
            access_token = await response.json();
            notify.success(`Вы успешно авторизировались!`);
        }
        else {
            notify.error(await response.json());
        }

        return response;
    }

    async function Register(data) {
        const response = await fetch(`/api/auth/register`, {
            method: 'POST',
            credentials: 'same-origin', // Если используете HttpOnly куки для аутентификации то include
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });

        if (response.status === 200) {
            access_token = await response.json();
        }
        else {
            log(await response.json());
        }

        return response;
    }

    async function Logout() {
        const response = await fetch(`/api/auth/logout`, {
            method: 'POST',
            credentials: 'same-origin', // Если используете HttpOnly куки для аутентификации то include
            headers: {
                "Authorization": `Bearer ${access_token}`,
            }
        });
        return response;
    }

    async function RefreshToken() {
        const response = await fetch(`/api/auth/refresh`, {
            method: 'GET',
            credentials: 'same-origin', // Если используете HttpOnly куки для аутентификации то include
        });

        if (response.status === 200) {
            access_token = await response.json();
        }
        else {
            log(await response.json());
        }

        return response;
    }

    async function Status() {
        const response = await fetch(`/api/auth/status`, {
            method: 'GET',
            credentials: 'same-origin', // Если используете HttpOnly куки для аутентификации то include
        });

        status = await response.json();

        return response;
    }

    async function GetInfo() {
        const response = await fetch(`/api/users/get?login=${status.login}`, {
            method: 'GET',
            credentials: 'same-origin', // Если используете HttpOnly куки для аутентификации то include
        });

        user_info = await response.json();

        return response;
    }

    async function AutoRefreshToken(func) {
        const response = await func();
        if (response.status !== 200) {
            const resRef = await RefreshToken();
            if (resRef.status !== 200) {
                status = null;
                access_token = null;

                notify.error('Необходимо заново войти в аккаунт!');
                UpdateAuthUI();
                return;
            }
            return await func();
        }

        return response;
    }

    async function OnLogoutBtnPressed() {
        const response = await AutoRefreshToken(Logout);

        if (response.status !== 200) {
            notify.error('Не удалось выйти из аккаунта (возможно ошибка соединения)\nПовторите еще раз');
            return;
        }

        status = null;
        access_token = null;
        user_info = null;

        if (document.activeElement) {
            document.activeElement.blur(); // Убирает фокус с текущего элемента
        }
        const offcanvas = bootstrap.Offcanvas.getInstance(elements.profile.offcanvas);
        offcanvas.hide();
        
        for (let i of onLogout) {
            i();
        }

        UpdateAuthUI();

        notify.success('Вы успешно вышли из аккаунта!');
    }

    function OnLoginBtnPressed() {
        is_registering = false;
        UpdateLoginFormUI();
    }

    function OnRegisterBtnPressed() {
        is_registering = true;
        UpdateLoginFormUI();
    }

    function OnChangeFormBtnPressed() {
        is_registering = !is_registering;
        UpdateLoginFormUI();
    }

    function Logged() {
        HideElement(elements.auth_buttons);
        ShowElement(elements.profile_button);
    }

    function NotLogged() {
        HideElement(elements.profile_button);
        ShowElement(elements.auth_buttons);
    }

    function ShowElement(el) {
        el.classList.remove('d-none');
    }

    function HideElement(el) {
        el.classList.add('d-none');
    }

    async function CheckLoginStatus() {
        await Status();

        if (status.isAuth) {
            await RefreshToken();
            await GetInfo();
        }
    }

    async function ExecuteWithToken(url, options) {
        const func = async () => {
            options.headers['Authorization'] = `Bearer ${access_token}`;
            options.headers['credentials'] = 'same-origin';
            const response = await fetch(url, options);

            return response;
        };

        return AutoRefreshToken(func);
    }

    window.document.addEventListener("DOMContentLoaded", OnDomLoaded);

    return {
        isAuth: () => status?.isAuth || false,
        isAdmin: () => status?.isAdmin || false,
        onUpdateAuthUI: onUpdateAuthUI,
        onLogout: onLogout,
        wallet: () => user_info?.wallet || 0,
        ExecuteUrlWithToken: ExecuteWithToken,
    }
})();
