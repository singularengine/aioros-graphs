import {select} from "d3-selection";

const svg = "http://www.w3.org/2000/svg";

class Element {
  constructor(document, name) {
    this.ownerDocument = document;
    this.namespaceURI = svg;
    this.tagName = name;
    this.attributes = [];
    this.childNodes = [];
    this.parentNode = null;
    this.text = null;
  }
  setAttribute(name, value) {
    const found = this.attributes.find((a) => a.name === name);
    if (found) found.value = String(value);
    else this.attributes.push({name, value: String(value)});
  }
  getAttribute(name) {
    const found = this.attributes.find((a) => a.name === name);
    return found ? found.value : null;
  }
  removeAttribute(name) {
    this.attributes = this.attributes.filter((a) => a.name !== name);
  }
  set textContent(value) {
    this.childNodes = [];
    this.text = String(value);
  }
  get textContent() {
    return this.text ?? this.childNodes.map((c) => c.textContent).join("");
  }
  insertBefore(child, next) {
    if (child.parentNode) child.parentNode.removeChild(child);
    const i = next == null ? -1 : this.childNodes.indexOf(next);
    if (i < 0) this.childNodes.push(child);
    else this.childNodes.splice(i, 0, child);
    child.parentNode = this;
    return child;
  }
  appendChild(child) {
    return this.insertBefore(child, null);
  }
  removeChild(child) {
    this.childNodes = this.childNodes.filter((c) => c !== child);
    child.parentNode = null;
    return child;
  }
  matches(selector) {
    if (selector === "*") return true;
    return selector.startsWith(".") ? (this.getAttribute("class") ?? "").split(" ").includes(selector.slice(1)) : this.tagName === selector;
  }
  querySelectorAll(selector) {
    const found = [];
    const visit = (node) => {
      for (const child of node.childNodes) {
        if (child.matches(selector)) found.push(child);
        visit(child);
      }
    };
    visit(this);
    return found;
  }
  querySelector(selector) {
    return this.querySelectorAll(selector)[0] ?? null;
  }
  compareDocumentPosition(other) {
    const all = this.ownerDocument.documentElement.querySelectorAll("*");
    return all.indexOf(this) < all.indexOf(other) ? 4 : 2;
  }
  get outerHTML() {
    const attributes = this.attributes.map((a) => ` ${a.name}="${escape(a.value, true)}"`).join("");
    const inner = this.text != null ? escape(this.text, false) : this.childNodes.map((c) => c.outerHTML).join("");
    return `<${this.tagName}${attributes}>${inner}</${this.tagName}>`;
  }
}

function escape(text, attribute) {
  const escaped = text.replaceAll("&", "&amp;").replaceAll(" ", "&nbsp;");
  return attribute ? escaped.replaceAll("\"", "&quot;") : escaped.replaceAll("<", "&lt;").replaceAll(">", "&gt;");
}

export function render(axis) {
  const document = {createElementNS: (uri, name) => new Element(document, name)};
  document.documentElement = new Element(document, "svg");
  const g = new Element(document, "g");
  document.documentElement.appendChild(g);
  select(g).call(axis);
  return {
    markup: g.outerHTML,
    domainPath: g.querySelector(".domain").getAttribute("d"),
    transforms: g.querySelectorAll(".tick").map((t) => t.getAttribute("transform")),
    labels: g.querySelectorAll(".tick").map((t) => t.querySelector("text").textContent),
  };
}
