export function setPointerCapture(elementId, pointerId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.setPointerCapture(pointerId);
    }
}

export function releasePointerCapture(elementId, pointerId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.releasePointerCapture(pointerId);
    }
}

export function getElementIdAtPoint(x, y) {
    const element = document.elementFromPoint(x, y);
    // Return the id or find the first parent with an id if the child (like the image) was hit
    let target = element;
    while (target && !target.id) {
        target = target.parentElement;
    }
    return target ? target.id : null;
}
