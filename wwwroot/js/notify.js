/**
 * Менеджер уведомлений на базе Bootstrap 5 Toasts
 * Новые уведомления появляются сверху
 */
class NotificationManager {
    constructor() {
        this.containerId = 'notification-container';
        this._ensureContainerExists();
    }

    /**
     * Создает контейнер для уведомлений, если его нет в DOM
     * @private
     */
    _ensureContainerExists() {
        if (!document.getElementById(this.containerId)) {
            const container = document.createElement('div');
            container.id = this.containerId;

            // Стили для позиционирования и поведения
            container.style.position = 'fixed';
            container.style.top = '20px';
            container.style.right = '20px';
            container.style.zIndex = '1090'; // Выше модальных окон Bootstrap
            container.style.display = 'flex';
            container.style.flexDirection = 'column';
            container.style.gap = '10px'; // Отступ между уведомлениями

            // Добавляем плавную анимацию появления для всего контейнера
            container.style.pointerEvents = 'none'; // Чтобы клики проходили сквозь пустой контейнер

            document.body.appendChild(container);
        }
    }

    /**
     * Показывает уведомление
     * 
     * @param {string} message - Текст сообщения
     * @param {string} type - Тип: 'success', 'error', 'warning', 'info'
     * @param {Object} options - Дополнительные настройки
     * @param {string} [options.title] - Заголовок уведомления
     * @param {number} [options.duration=5000] - Время показа в мс (0 = не скрывать автоматически)
     * @param {boolean} [options.showCloseButton=true] - Показывать ли кнопку закрытия
     */
    show(message, type = 'info', options = {}) {
        const {
            title = '',
            duration = 5000,
            showCloseButton = true
        } = options;

        const typeConfig = {
            success: { bgClass: 'bg-success', icon: '✅', titleDefault: 'Успешно' },
            error: { bgClass: 'bg-danger', icon: '❌', titleDefault: 'Ошибка' },
            warning: { bgClass: 'bg-warning text-dark', icon: '⚠️', titleDefault: 'Внимание' },
            info: { bgClass: 'bg-info text-dark', icon: 'ℹ️', titleDefault: 'Информация' }
        };

        const config = typeConfig[type] || typeConfig.info;
        const finalTitle = title || config.titleDefault;

        const toastEl = document.createElement('div');
        toastEl.className = `toast align-items-center border-0 shadow-sm ${config.bgClass}`;
        toastEl.setAttribute('role', 'alert');
        toastEl.setAttribute('aria-live', 'assertive');
        toastEl.setAttribute('aria-atomic', 'true');

        // Разрешаем клики по самому уведомлению (так как у контейнера pointer-events: none)
        toastEl.style.pointerEvents = 'auto';

        const closeButtonHtml = showCloseButton
            ? `<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>`
            : '';

        const titleHtml = finalTitle
            ? `<strong class="me-auto d-flex align-items-center gap-2"><span>${config.icon}</span> <span>${finalTitle}</span></strong>`
            : `<strong class="me-auto d-flex align-items-center gap-2"><span>${config.icon}</span></strong>`;

        toastEl.innerHTML = `
            <div class="d-flex">
                <div class="toast-body">
                    ${titleHtml}
                    <div class="mt-1">${message}</div>
                </div>
                ${closeButtonHtml}
            </div>
        `;

        const container = document.getElementById(this.containerId);

        // prepend добавляет элемент в НАЧАЛО контейнера (сверху)
        container.prepend(toastEl);

        const toastOptions = {
            autohide: duration > 0,
            delay: duration
        };

        const bsToast = new bootstrap.Toast(toastEl, toastOptions);
        bsToast.show();

        // Удаляем элемент из DOM после скрытия
        toastEl.addEventListener('hidden.bs.toast', () => {
            toastEl.remove();
        });
    }

    // --- Удобные сокращения (API) ---
    success(message, options = {}) { this.show(message, 'success', options); }
    error(message, options = {}) { this.show(message, 'error', options); }
    warning(message, options = {}) { this.show(message, 'warning', options); }
    info(message, options = {}) { this.show(message, 'info', options); }
}

const notify = new NotificationManager();
// export default notify; // Раскомментируйте для ES6 модулей