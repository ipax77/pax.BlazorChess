
var blazorModal = null;

function getElementSize(elementId) {
    var element = document.getElementById(elementId);
    if (element != null) {
        var size = element.getBoundingClientRect();
        return [size.x, size.y, size.width, size.height];
    }
};

function reportSize() {
    var size = getElementSize('board');
    window.Dotnet.invokeMethodAsync('ResizeEvent', size);
}

function resizeEvents(ref, toggle) {
    if (toggle) {
        window.Dotnet = ref;
        window.onresize = reportSize;
    }
    else {
        window.Dotnet = null;
        window.onresize = null;
    }
}

function modalClose() {
    if (blazorModal) {
        blazorModal.hide();
    }
}

function modalOpen(name) {

    const modalElement = document.getElementById(name);

    if (!modalElement) {
        return;
    }

    blazorModal = new bootstrap.Modal(modalElement);
    blazorModal.show();
}

function promoteModalOpen(ref) {
    promoteRef = ref;
    const modalElement = document.getElementById('promoteModal');
    if (!modalElement) {
        return;
    }

    Modal = new bootstrap.Modal(modalElement);
    Modal.show();
    modalElement.addEventListener('hide.bs.modal', function () {
        promoteRef.invokeMethodAsync('PromoteClose');
        this.removeEventListener('hide.bs.modal', arguments.callee);
        promoteRef = null;
    });
}