function setPointerCapture(elementId, pointerId) {
    var element = document.getElementById(elementId);
    if (!element) {
        return;
    }
    element.setPointerCapture(pointerId);
}