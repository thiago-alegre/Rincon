function rinconConfirm(options) {
    return Swal.fire({
        title: options.title,
        text: options.text,
        icon: options.icon || "question",
        showCancelButton: true,
        confirmButtonColor: "#0d6efd",
        cancelButtonColor: "#6c757d",
        confirmButtonText: options.confirmButtonText || "Si, continuar",
        cancelButtonText: options.cancelButtonText || "Cancelar",
        buttonsStyling: true,
        customClass: {
            popup: "rincon-swal-popup",
            title: "rincon-swal-title",
            confirmButton: "rincon-swal-confirm",
            cancelButton: "rincon-swal-cancel"
        }
    });
}
