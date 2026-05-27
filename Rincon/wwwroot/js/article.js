var dataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblArticles').DataTable({
        pageLength: 5,
        ajax: {
            url: "/Admin/Articles/GetAll"
        },
        columns: [
            {
                data: "imageUrl",
                width: "8%",
                render: function (data) {
                    if (data) {
                        return `
                            <div class="text-center">
                                <img src="${data}" class="product-img-sm" alt="Imagen del artículo" />
                            </div>
                        `;
                    }

                    return `<span class="text-muted">Sin imagen</span>`;
                }
            },
            { data: "name", width: "14%" },
            { data: "code", width: "10%" },
            {
                data: "category.name",
                width: "12%",
                render: function (data) {
                    return data ?? "";
                }
            },
            {
                data: "price",
                width: "10%",
                render: function (data, type, row) {
                    if (data == null) return "";

                    let suffix = getPriceSuffix(row);

                    return "$ " + formatNumber(data, 2) + suffix;
                }
            },
            {
                data: "cost",
                width: "10%",
                render: function (data, type, row) {
                    if (data == null) return "";

                    let suffix = getPriceSuffix(row);

                    return "$ " + formatNumber(data, 2) + suffix;
                }
            },
            {
                data: "stock",
                width: "10%",
                render: function (data, type, row) {
                    if (data == null) return "";

                    const stockText = formatStock(data, row);
                    const isLowStock = row.stockMin != null && Number(data) <= Number(row.stockMin);

                    if (isLowStock) {
                        return `<span class="stock-badge stock-low">${stockText}</span>`;
                    }

                    return `<span>${stockText}</span>`;
                }
            },
            {
                data: "expirationDate",
                width: "12%",
                render: function (data) {
                    return renderExpirationDate(data);
                }
            },
            {
                data: "isActive",
                width: "5%",
                render: function (data) {
                    if (data === true) {
                        return `<span class="status-badge status-active">Activo</span>`;
                    }

                    return `<span class="status-badge status-inactive">Inactivo</span>`;
                }
            },
            {
                data: "id",
                width: "16%",
                render: function (data) {
                    return `
                        <div class="datatable-action-group">
                            <a href="/Admin/Articles/Upsert/${data}" class="btn btn-soft-success btn-modern-sm" title="Editar artículo">
                                <i class="fa fa-pencil" aria-hidden="true"></i>
                            </a>
                            <a onclick=Delete("/Admin/Articles/Delete/${data}") class="btn btn-soft-danger btn-modern-sm" title="Eliminar artículo">
                                <i class="fa fa-trash" aria-hidden="true"></i>
                            </a>
                        </div>
                    `;
                }
            }
        ],
        language: {
            decimal: "",
            emptyTable: "No hay datos disponibles",
            info: "Mostrando _START_ a _END_ de _TOTAL_ registros",
            infoEmpty: "Mostrando 0 a 0 de 0 registros",
            infoFiltered: "(filtrado de _MAX_ registros totales)",
            lengthMenu: "Mostrar _MENU_ registros",
            loadingRecords: "Cargando...",
            processing: "Procesando...",
            search: "Buscar:",
            zeroRecords: "No se encontraron registros",
            paginate: {
                first: "Primero",
                last: "Último",
                next: "Siguiente",
                previous: "Anterior"
            }
        }
    });
}

function formatNumber(value, decimals = 2) {
    return Number(value).toLocaleString("es-AR", {
        minimumFractionDigits: decimals,
        maximumFractionDigits: decimals
    });
}

function formatSmartNumber(value, maxDecimals = 2) {
    const number = Number(value);

    if (Number.isInteger(number)) {
        return number.toLocaleString("es-AR", {
            maximumFractionDigits: 0
        });
    }

    return number.toLocaleString("es-AR", {
        minimumFractionDigits: 0,
        maximumFractionDigits: maxDecimals
    });
}

function formatStock(value, row) {
    const stock = Number(value);
    const unit = row.unitOfMeasure;
    const isSoldByWeight = row.isSoldByWeight === true;

    if (!isSoldByWeight || unit === "Unidad") {
        return `${formatSmartNumber(stock, 0)} u`;
    }

    return `${formatSmartNumber(stock, 2)} kg`;
}

function getPriceSuffix(row) {
    const unit = row.unitOfMeasure;
    const isSoldByWeight = row.isSoldByWeight === true;

    if (!isSoldByWeight || unit === "Unidad") {
        return " / u";
    }

    return " / kg";
}

function renderExpirationDate(data) {
    if (!data) {
        return `<span class="expiration-badge expiration-empty">Sin vencimiento</span>`;
    }

    const expirationDate = new Date(data);
    const today = new Date();

    expirationDate.setHours(0, 0, 0, 0);
    today.setHours(0, 0, 0, 0);

    const diffTime = expirationDate.getTime() - today.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

    const formattedDate = expirationDate.toLocaleDateString("es-AR");

    if (diffDays < 0) {
        return `<span class="expiration-badge expiration-danger">Vencido - ${formattedDate}</span>`;
    }

    if (diffDays <= 7) {
        return `<span class="expiration-badge expiration-warning">Por vencer - ${formattedDate}</span>`;
    }

    return `<span class="expiration-badge expiration-ok">Vigente - ${formattedDate}</span>`;
}

function Delete(url) {
    rinconConfirm({
        title: "Eliminar artículo",
        text: "El artículo se eliminará permanentemente",
        icon: "question",
        confirmButtonText: "Sí, eliminar",
        cancelButtonText: "Cancelar"
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                type: "DELETE",
                url: url,
                success: function (data) {
                    if (data.success) {
                        dataTable.ajax.reload();
                        toastr.success(data.message);
                    } else {
                        toastr.error(data.message);
                    }
                },
                error: function () {
                    toastr.error("Ocurrió un error al intentar eliminar el artículo");
                }
            });
        }
    });
}
