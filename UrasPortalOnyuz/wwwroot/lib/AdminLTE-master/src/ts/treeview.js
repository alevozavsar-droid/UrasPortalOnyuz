/**
 * --------------------------------------------
 * @file AdminLTE treeview.ts
 * @description Treeview plugin for AdminLTE.
 * @license MIT
 * --------------------------------------------
 */
import { onDOMContentLoaded, slideDown, slideUp } from './util/index';
/**
 * ------------------------------------------------------------------------
 * Constants
 * ------------------------------------------------------------------------
 */
// const NAME = 'Treeview'
const DATA_KEY = 'lte.treeview';
const EVENT_KEY = `.${DATA_KEY}`;
const EVENT_EXPANDED = `expanded${EVENT_KEY}`;
const EVENT_COLLAPSED = `collapsed${EVENT_KEY}`;
// const EVENT_LOAD_DATA_API = `load${EVENT_KEY}`
const CLASS_NAME_MENU_OPEN = 'menu-open';
const SELECTOR_NAV_ITEM = '.nav-item';
const SELECTOR_NAV_LINK = '.nav-link';
const SELECTOR_TREEVIEW_MENU = '.nav-treeview';
const SELECTOR_DATA_TOGGLE = '[data-lte-toggle="treeview"]';
const Default = {
    animationSpeed: 300,
    accordion: true
};
/**
 * Class Definition
 * ====================================================
 */
class Treeview {
    constructor(element, config) {
        this._element = element;
        this._config = Object.assign(Object.assign({}, Default), config);
    }
    open() {
        var _a, _b;
        const event = new Event(EVENT_EXPANDED);
        if (this._config.accordion) {
            const openMenuList = (_a = this._element.parentElement) === null || _a === void 0 ? void 0 : _a.querySelectorAll(`${SELECTOR_NAV_ITEM}.${CLASS_NAME_MENU_OPEN}`);
            openMenuList === null || openMenuList === void 0 ? void 0 : openMenuList.forEach(openMenu => {
                if (openMenu !== this._element.parentElement) {
                    openMenu.classList.remove(CLASS_NAME_MENU_OPEN);
                    const childElement = openMenu === null || openMenu === void 0 ? void 0 : openMenu.querySelector(SELECTOR_TREEVIEW_MENU);
                    if (childElement) {
                        slideUp(childElement, this._config.animationSpeed);
                    }
                }
            });
        }
        this._element.classList.add(CLASS_NAME_MENU_OPEN);
        const childElement = (_b = this._element) === null || _b === void 0 ? void 0 : _b.querySelector(SELECTOR_TREEVIEW_MENU);
        if (childElement) {
            slideDown(childElement, this._config.animationSpeed);
        }
        this._element.dispatchEvent(event);
    }
    close() {
        var _a;
        const event = new Event(EVENT_COLLAPSED);
        this._element.classList.remove(CLASS_NAME_MENU_OPEN);
        const childElement = (_a = this._element) === null || _a === void 0 ? void 0 : _a.querySelector(SELECTOR_TREEVIEW_MENU);
        if (childElement) {
            slideUp(childElement, this._config.animationSpeed);
        }
        this._element.dispatchEvent(event);
    }
    toggle() {
        if (this._element.classList.contains(CLASS_NAME_MENU_OPEN)) {
            this.close();
        }
        else {
            this.open();
        }
    }
}
/**
 * ------------------------------------------------------------------------
 * Data Api implementation
 * ------------------------------------------------------------------------
 */
onDOMContentLoaded(() => {
    const button = document.querySelectorAll(SELECTOR_DATA_TOGGLE);
    button.forEach(btn => {
        btn.addEventListener('click', event => {
            const target = event.target;
            const targetItem = target.closest(SELECTOR_NAV_ITEM);
            const targetLink = target.closest(SELECTOR_NAV_LINK);
            if ((target === null || target === void 0 ? void 0 : target.getAttribute('href')) === '#' || (targetLink === null || targetLink === void 0 ? void 0 : targetLink.getAttribute('href')) === '#') {
                event.preventDefault();
            }
            if (targetItem) {
                const data = new Treeview(targetItem, Default);
                data.toggle();
            }
        });
    });
});
export default Treeview;
