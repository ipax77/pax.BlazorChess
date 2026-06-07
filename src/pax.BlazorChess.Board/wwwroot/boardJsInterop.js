export function setPointerCapture(elementId, pointerId) {
    const element = document.getElementById(elementId);
    if (!element) {
        return;
    }
    updateBoardSize(elementId);
    element.setPointerCapture(pointerId);
}

export function releasePointerCapture(elementId, pointerId) {
    const element = document.getElementById(elementId);
    if (!element) {
        return;
    }

    if (element.hasPointerCapture(pointerId)) {
        element.releasePointerCapture(pointerId);
    }
}

export function getSquareIndexFromPoint(clientX, clientY, boardGuid) {
    const target = document.elementFromPoint(clientX, clientY);
    if (!target) {
        return null;
    }

    const squareElement = target.closest(`[id^="${boardGuid}_"]`);
    if (!squareElement) {
        return null;
    }

    const match = squareElement.id.match(/_(\d+)$/);
    if (!match) {
        return null;
    }

    const index = Number.parseInt(match[1], 10);
    return Number.isNaN(index) ? null : index;
}

export function updateBoardSize(boardId) {
    const board = document.getElementById(boardId);
    if (!board) return;

    const rect = board.getBoundingClientRect();
    board.style.setProperty("--board-size", rect.width + "px");
}

const pendingScrolls = new Map();
const navigationKeyHandlers = new Map();
const handledNavigationKeys = new Set(["ArrowLeft", "ArrowRight", " ", "Space", "Spacebar"]);

export function scrollToElement(elementId, behavior = "auto") {
    const element = document.getElementById(elementId);
    if (!element) {
        return;
    }

    const container = element.closest(".table-responsive");
    if (!container) {
        return;
    }

    const scrollKey = container?.id || elementId;
    const pending = pendingScrolls.get(scrollKey);
    if (pending) {
        cancelAnimationFrame(pending.frame);
    }

    const frame = requestAnimationFrame(() => {
        pendingScrolls.delete(scrollKey);
        const target = document.getElementById(elementId);
        const currentContainer = target?.closest(".table-responsive");
        if (target && currentContainer) {
            scrollElementInsideContainer(currentContainer, target, behavior);
        }
    });

    pendingScrolls.set(scrollKey, { frame, elementId, behavior });
}

function scrollElementInsideContainer(container, element, behavior) {
    const containerRect = container.getBoundingClientRect();
    const elementRect = element.getBoundingClientRect();
    const elementTop = elementRect.top - containerRect.top + container.scrollTop;
    const centeredTop = elementTop - ((container.clientHeight - elementRect.height) / 2);
    const maxTop = Math.max(0, container.scrollHeight - container.clientHeight);
    const top = Math.min(Math.max(0, centeredTop), maxTop);

    if (behavior === "smooth" && typeof container.scrollTo === "function") {
        container.scrollTo({ top, behavior });
    } else {
        container.scrollTop = top;
    }
}

export function preventNavigationKeyDefaults(elementId) {
    if (navigationKeyHandlers.has(elementId)) {
        return;
    }

    const element = document.getElementById(elementId);
    if (!element) {
        return;
    }

    const handler = event => {
        if (!handledNavigationKeys.has(event.key) || isTextEntryTarget(event.target)) {
            return;
        }

        event.preventDefault();
    };

    element.addEventListener("keydown", handler, { capture: true });
    navigationKeyHandlers.set(elementId, { element, handler });
}

export function releaseNavigationKeyDefaults(elementId) {
    const registration = navigationKeyHandlers.get(elementId);
    if (!registration) {
        return;
    }

    registration.element.removeEventListener("keydown", registration.handler, true);
    navigationKeyHandlers.delete(elementId);
}

function isTextEntryTarget(target) {
    if (!(target instanceof Element)) {
        return false;
    }

    return target.closest("input, textarea, select, [contenteditable]") !== null;
}

export function scrollToBottom(elementId, containerId) {
    const element = document.getElementById(elementId);
    const container = document.getElementById(containerId);
    if (!element || !container) {
        return;
    }
    const isAtBottom = container.scrollHeight - container.scrollTop <= container.clientHeight + 50;
    if (isAtBottom) {
        element.scrollIntoView({ behavior: "smooth", block: "end" });
    }
}
