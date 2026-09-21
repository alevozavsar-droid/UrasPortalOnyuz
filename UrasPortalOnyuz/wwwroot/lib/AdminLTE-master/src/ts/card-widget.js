/**
 * --------------------------------------------
 * @file AdminLTE card-widget.ts
 * @description Card widget for AdminLTE.
 * @license MIT
 * --------------------------------------------
 */
import { onDOMContentLoaded, slideUp, slideDown } from './util/index';
/**
 * Constants
 * ====================================================
 */
const DATA_KEY = 'lte.card-widget';
const EVENT_KEY = `.${DATA_KEY}`;
const EVENT_COLLAPSED = `collapsed${EVENT_KEY}`;
const EVENT_EXPANDED = `expanded${EVENT_KEY}`;
const EVENT_REMOVE = `remove${EVENT_KEY}`;
const EVENT_MAXIMIZED = `maximized${EVENT_KEY}`;
const EVENT_MINIMIZED = `minimized${EVENT_KEY}`;
const CLASS_NAME_CARD = 'card';
const CLASS_NAME_COLLAPSED = 'collapsed-card';
const CLASS_NAME_COLLAPSING = 'collapsing-card';
const CLASS_NAME_EXPANDING = 'expanding-card';
const CLASS_NAME_WAS_COLLAPSED = 'was-collapsed';
const CLASS_NAME_MAXIMIZED = 'maximized-card';
const SELECTOR_DATA_REMOVE = '[data-lte-toggle="card-remove"]';
const SELECTOR_DATA_COLLAPSE = '[data-lte-toggle="card-collapse"]';
const SELECTOR_DATA_MAXIMIZE = '[data-lte-toggle="card-maximize"]';
const SELECTOR_CARD = `.${CLASS_NAME_CARD}`;
const SELECTOR_CARD_BODY = '.card-body';
const SELECTOR_CARD_FOOTER = '.card-footer';
const Default = {
    animationSpeed: 500,
    collapseTrigger: SELECTOR_DATA_COLLAPSE,
    removeTrigger: SELECTOR_DATA_REMOVE,
    maximizeTrigger: SELECTOR_DATA_MAXIMIZE
};
class CardWidget {
    constructor(element, config) {
        this._element = element;
        this._parent = element.closest(SELECTOR_CARD);
        if (element.classList.contains(CLASS_NAME_CARD)) {
            this._parent = element;
        }
        this._config = Object.assign(Object.assign({}, Default), config);
    }
    collapse() {
        var _a, _b;
        const event = new Event(EVENT_COLLAPSED);
        if (this._parent) {
            this._parent.classList.add(CLASS_NAME_COLLAPSING);
            const elm = (_a = this._parent) === null || _a === void 0 ? void 0 : _a.querySelectorAll(`${SELECTOR_CARD_BODY}, ${SELECTOR_CARD_FOOTER}`);
            elm.forEach(el => {
                if (el instanceof HTMLElement) {
                    slideUp(el, this._config.animationSpeed);
                }
            });
            setTimeout(() => {
                if (this._parent) {
                    this._parent.classList.add(CLASS_NAME_COLLAPSED);
                    this._parent.classList.remove(CLASS_NAME_COLLAPSING);
                }
            }, this._config.animationSpeed);
        }
        (_b = this._element) === null || _b === void 0 ? void 0 : _b.dispatchEvent(event);
    }
    expand() {
        var _a, _b;
        const event = new Event(EVENT_EXPANDED);
        if (this._parent) {
            this._parent.classList.add(CLASS_NAME_EXPANDING);
            const elm = (_a = this._parent) === null || _a === void 0 ? void 0 : _a.querySelectorAll(`${SELECTOR_CARD_BODY}, ${SELECTOR_CARD_FOOTER}`);
            elm.forEach(el => {
                if (el instanceof HTMLElement) {
                    slideDown(el, this._config.animationSpeed);
                }
            });
            setTimeout(() => {
                if (this._parent) {
                    this._parent.classList.remove(CLASS_NAME_COLLAPSED);
                    this._parent.classList.remove(CLASS_NAME_EXPANDING);
                }
            }, this._config.animationSpeed);
        }
        (_b = this._element) === null || _b === void 0 ? void 0 : _b.dispatchEvent(event);
    }
    remove() {
        var _a;
        const event = new Event(EVENT_REMOVE);
        if (this._parent) {
            slideUp(this._parent, this._config.animationSpeed);
        }
        (_a = this._element) === null || _a === void 0 ? void 0 : _a.dispatchEvent(event);
    }
    toggle() {
        var _a;
        if ((_a = this._parent) === null || _a === void 0 ? void 0 : _a.classList.contains(CLASS_NAME_COLLAPSED)) {
            this.expand();
            return;
        }
        this.collapse();
    }
    maximize() {
        var _a;
        const event = new Event(EVENT_MAXIMIZED);
        if (this._parent) {
            this._parent.style.height = `${this._parent.offsetHeight}px`;
            this._parent.style.width = `${this._parent.offsetWidth}px`;
            this._parent.style.transition = 'all .15s';
            setTimeout(() => {
                const htmlTag = document.querySelector('html');
                if (htmlTag) {
                    htmlTag.classList.add(CLASS_NAME_MAXIMIZED);
                }
                if (this._parent) {
                    this._parent.classList.add(CLASS_NAME_MAXIMIZED);
                    if (this._parent.classList.contains(CLASS_NAME_COLLAPSED)) {
                        this._parent.classList.add(CLASS_NAME_WAS_COLLAPSED);
                    }
                }
            }, 150);
        }
        (_a = this._element) === null || _a === void 0 ? void 0 : _a.dispatchEvent(event);
    }
    minimize() {
        var _a;
        const event = new Event(EVENT_MINIMIZED);
        if (this._parent) {
            this._parent.style.height = 'auto';
            this._parent.style.width = 'auto';
            this._parent.style.transition = 'all .15s';
            setTimeout(() => {
                var _a;
                const htmlTag = document.querySelector('html');
                if (htmlTag) {
                    htmlTag.classList.remove(CLASS_NAME_MAXIMIZED);
                }
                if (this._parent) {
                    this._parent.classList.remove(CLASS_NAME_MAXIMIZED);
                    if ((_a = this._parent) === null || _a === void 0 ? void 0 : _a.classList.contains(CLASS_NAME_WAS_COLLAPSED)) {
                        this._parent.classList.remove(CLASS_NAME_WAS_COLLAPSED);
                    }
                }
            }, 10);
        }
        (_a = this._element) === null || _a === void 0 ? void 0 : _a.dispatchEvent(event);
    }
    toggleMaximize() {
        var _a;
        if ((_a = this._parent) === null || _a === void 0 ? void 0 : _a.classList.contains(CLASS_NAME_MAXIMIZED)) {
            this.minimize();
            return;
        }
        this.maximize();
    }
}
/**
 *
 * Data Api implementation
 * ====================================================
 */
onDOMContentLoaded(() => {
    const collapseBtn = document.querySelectorAll(SELECTOR_DATA_COLLAPSE);
    collapseBtn.forEach(btn => {
        btn.addEventListener('click', event => {
            event.preventDefault();
            const target = event.target;
            const data = new CardWidget(target, Default);
            data.toggle();
        });
    });
    const removeBtn = document.querySelectorAll(SELECTOR_DATA_REMOVE);
    removeBtn.forEach(btn => {
        btn.addEventListener('click', event => {
            event.preventDefault();
            const target = event.target;
            const data = new CardWidget(target, Default);
            data.remove();
        });
    });
    const maxBtn = document.querySelectorAll(SELECTOR_DATA_MAXIMIZE);
    maxBtn.forEach(btn => {
        btn.addEventListener('click', event => {
            event.preventDefault();
            const target = event.target;
            const data = new CardWidget(target, Default);
            data.toggleMaximize();
        });
    });
});
export default CardWidget;
