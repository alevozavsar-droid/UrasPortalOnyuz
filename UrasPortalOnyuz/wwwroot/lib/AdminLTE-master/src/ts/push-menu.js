/**
 * --------------------------------------------
 * @file AdminLTE push-menu.ts
 * @description Push menu for AdminLTE.
 * @license MIT
 * --------------------------------------------
 */
import { onDOMContentLoaded } from './util/index';
/**
 * ------------------------------------------------------------------------
 * Constants
 * ------------------------------------------------------------------------
 */
const DATA_KEY = 'lte.push-menu';
const EVENT_KEY = `.${DATA_KEY}`;
const EVENT_OPEN = `open${EVENT_KEY}`;
const EVENT_COLLAPSE = `collapse${EVENT_KEY}`;
const CLASS_NAME_SIDEBAR_MINI = 'sidebar-mini';
const CLASS_NAME_SIDEBAR_COLLAPSE = 'sidebar-collapse';
const CLASS_NAME_SIDEBAR_OPEN = 'sidebar-open';
const CLASS_NAME_SIDEBAR_EXPAND = 'sidebar-expand';
const CLASS_NAME_SIDEBAR_OVERLAY = 'sidebar-overlay';
const CLASS_NAME_MENU_OPEN = 'menu-open';
const SELECTOR_APP_SIDEBAR = '.app-sidebar';
const SELECTOR_SIDEBAR_MENU = '.sidebar-menu';
const SELECTOR_NAV_ITEM = '.nav-item';
const SELECTOR_NAV_TREEVIEW = '.nav-treeview';
const SELECTOR_APP_WRAPPER = '.app-wrapper';
const SELECTOR_SIDEBAR_EXPAND = `[class*="${CLASS_NAME_SIDEBAR_EXPAND}"]`;
const SELECTOR_SIDEBAR_TOGGLE = '[data-lte-toggle="sidebar"]';
const Defaults = {
    sidebarBreakpoint: 992
};
/**
 * Class Definition
 * ====================================================
 */
class PushMenu {
    constructor(element, config) {
        this._element = element;
        this._config = Object.assign(Object.assign({}, Defaults), config);
    }
    // TODO
    menusClose() {
        const navTreeview = document.querySelectorAll(SELECTOR_NAV_TREEVIEW);
        navTreeview.forEach(navTree => {
            navTree.style.removeProperty('display');
            navTree.style.removeProperty('height');
        });
        const navSidebar = document.querySelector(SELECTOR_SIDEBAR_MENU);
        const navItem = navSidebar === null || navSidebar === void 0 ? void 0 : navSidebar.querySelectorAll(SELECTOR_NAV_ITEM);
        if (navItem) {
            navItem.forEach(navI => {
                navI.classList.remove(CLASS_NAME_MENU_OPEN);
            });
        }
    }
    expand() {
        const event = new Event(EVENT_OPEN);
        document.body.classList.remove(CLASS_NAME_SIDEBAR_COLLAPSE);
        document.body.classList.add(CLASS_NAME_SIDEBAR_OPEN);
        this._element.dispatchEvent(event);
    }
    collapse() {
        const event = new Event(EVENT_COLLAPSE);
        document.body.classList.remove(CLASS_NAME_SIDEBAR_OPEN);
        document.body.classList.add(CLASS_NAME_SIDEBAR_COLLAPSE);
        this._element.dispatchEvent(event);
    }
    addSidebarBreakPoint() {
        var _a, _b, _c;
        const sidebarExpandList = (_b = (_a = document.querySelector(SELECTOR_SIDEBAR_EXPAND)) === null || _a === void 0 ? void 0 : _a.classList) !== null && _b !== void 0 ? _b : [];
        const sidebarExpand = (_c = Array.from(sidebarExpandList).find(className => className.startsWith(CLASS_NAME_SIDEBAR_EXPAND))) !== null && _c !== void 0 ? _c : '';
        const sidebar = document.getElementsByClassName(sidebarExpand)[0];
        const sidebarContent = window.getComputedStyle(sidebar, '::before').getPropertyValue('content');
        this._config = Object.assign(Object.assign({}, this._config), { sidebarBreakpoint: Number(sidebarContent.replace(/[^\d.-]/g, '')) });
        if (window.innerWidth <= this._config.sidebarBreakpoint) {
            this.collapse();
        }
        else {
            if (!document.body.classList.contains(CLASS_NAME_SIDEBAR_MINI)) {
                this.expand();
            }
            if (document.body.classList.contains(CLASS_NAME_SIDEBAR_MINI) && document.body.classList.contains(CLASS_NAME_SIDEBAR_COLLAPSE)) {
                this.collapse();
            }
        }
    }
    toggle() {
        if (document.body.classList.contains(CLASS_NAME_SIDEBAR_COLLAPSE)) {
            this.expand();
        }
        else {
            this.collapse();
        }
    }
    init() {
        this.addSidebarBreakPoint();
    }
}
/**
 * ------------------------------------------------------------------------
 * Data Api implementation
 * ------------------------------------------------------------------------
 */
onDOMContentLoaded(() => {
    var _a;
    const sidebar = document === null || document === void 0 ? void 0 : document.querySelector(SELECTOR_APP_SIDEBAR);
    if (sidebar) {
        const data = new PushMenu(sidebar, Defaults);
        data.init();
        window.addEventListener('resize', () => {
            data.init();
        });
    }
    const sidebarOverlay = document.createElement('div');
    sidebarOverlay.className = CLASS_NAME_SIDEBAR_OVERLAY;
    (_a = document.querySelector(SELECTOR_APP_WRAPPER)) === null || _a === void 0 ? void 0 : _a.append(sidebarOverlay);
    sidebarOverlay.addEventListener('touchstart', event => {
        event.preventDefault();
        const target = event.currentTarget;
        const data = new PushMenu(target, Defaults);
        data.collapse();
    }, { passive: true });
    sidebarOverlay.addEventListener('click', event => {
        event.preventDefault();
        const target = event.currentTarget;
        const data = new PushMenu(target, Defaults);
        data.collapse();
    });
    const fullBtn = document.querySelectorAll(SELECTOR_SIDEBAR_TOGGLE);
    fullBtn.forEach(btn => {
        btn.addEventListener('click', event => {
            event.preventDefault();
            let button = event.currentTarget;
            if ((button === null || button === void 0 ? void 0 : button.dataset.lteToggle) !== 'sidebar') {
                button = button === null || button === void 0 ? void 0 : button.closest(SELECTOR_SIDEBAR_TOGGLE);
            }
            if (button) {
                event === null || event === void 0 ? void 0 : event.preventDefault();
                const data = new PushMenu(button, Defaults);
                data.toggle();
            }
        });
    });
});
export default PushMenu;
