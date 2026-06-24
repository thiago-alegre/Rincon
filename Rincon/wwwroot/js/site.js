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

if (window.jQuery) {
    $.ajaxSetup({
        beforeSend: function (xhr, settings) {
            const method = (settings.type || settings.method || "GET").toUpperCase();

            if (!["POST", "PUT", "PATCH", "DELETE"].includes(method)) {
                return;
            }

            const token = document.querySelector('meta[name="request-verification-token"]')?.getAttribute("content");

            if (token) {
                xhr.setRequestHeader("RequestVerificationToken", token);
            }
        }
    });
}

function rinconModal(options) {
    const config = {
        title: options.title,
        text: options.text,
        icon: options.icon || "info",
        confirmButtonColor: "#0d6efd",
        confirmButtonText: options.confirmButtonText || "Entendido",
        buttonsStyling: true,
        customClass: {
            popup: "rincon-swal-popup",
            title: "rincon-swal-title",
            confirmButton: "rincon-swal-confirm"
        }
    };

    if (options.showCancelButton) {
        config.showCancelButton = true;
        config.cancelButtonColor = "#6c757d";
        config.cancelButtonText = options.cancelButtonText || "Cancelar";
        config.customClass.cancelButton = "rincon-swal-cancel";
    }

    return Swal.fire(config).then(function (result) {
        if (result.isConfirmed && options.confirmUrl) {
            window.location.href = options.confirmUrl;
        }

        return result;
    });
}
