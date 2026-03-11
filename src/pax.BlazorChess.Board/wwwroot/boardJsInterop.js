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
    const root = document.documentElement;
    root.style.setProperty("--board-size", rect.width + "px");
}

export function scrollToElement(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollIntoView({ behavior: "smooth", block: "center", inline: "center" });
    }
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