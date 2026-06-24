$(document).ready(function () {
    const table = $('#articleBatchesTable');
    const articleId = table.data('article-id');

    table.DataTable({
        serverSide: true,
        processing: true,
        pageLength: 5,
        ajax: {
            url: `/Admin/ArticleBatches/GetAll?articleId=${articleId}`,
            type: 'GET',
            datatype: 'json'
        },
        columns: [
            {
                data: 'purchaseDate',
                render: function (data) {
                    return formatDate(data);
                }
            },
            {
                data: 'expirationDate',
                render: function (data, type, row) {
                    return renderBatchExpiration(data, row.expirationDisplay);
                }
            },
            {
                data: 'quantity',
                render: function (data) {
                    return `${formatSmartNumber(data)} disp.`;
                }
            },
            {
                data: 'initialQuantity',
                render: function (data) {
                    return formatSmartNumber(data);
                }
            },
            {
                data: 'cost',
                render: function (data) {
                    return `$ ${formatMoney(data)}`;
                }
            },
            {
                data: 'isActive',
                render: function (data) {
                    return data
                        ? '<span class="status-badge status-active">Activo</span>'
                        : '<span class="status-badge status-inactive">Inactivo</span>';
                }
            },
            {
                data: null,
                orderable: false,
                searchable: false,
                render: function (data) {
                    return `
                        <div class="datatable-action-group justify-content-end">
                            <a href="/Admin/ArticleBatches/Upsert?articleId=${data.articleId}&id=${data.id}" class="btn btn-soft-secondary btn-modern-sm" title="Editar lote">
                                <i class="fa fa-pencil"></i>
                            </a>
                            <button type="button" class="btn btn-soft-danger btn-modern-sm" onclick="deleteBatch(${data.id})" title="Desactivar lote">
                                <i class="fa fa-ban"></i>
                            </button>
                        </div>
                    `;
                }
            }
        ],
        language: {
            url: '//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json'
        },
        order: [[1, 'asc']]
    });
});

function formatMoney(value) {
    return Number(value).toLocaleString('es-AR', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });
}

function formatSmartNumber(value) {
    const number = Number(value);

    if (Number.isInteger(number)) {
        return number.toLocaleString('es-AR', { maximumFractionDigits: 0 });
    }

    return number.toLocaleString('es-AR', {
        minimumFractionDigits: 0,
        maximumFractionDigits: 3
    });
}

function formatDate(value) {
    if (!value) {
        return '-';
    }

    return new Date(value).toLocaleDateString('es-AR');
}

function renderBatchExpiration(value, displayValue) {
    if (!value) {
        return '<span class="expiration-badge expiration-empty">Sin vencimiento</span>';
    }

    const expirationDate = new Date(value);
    const today = new Date();

    expirationDate.setHours(0, 0, 0, 0);
    today.setHours(0, 0, 0, 0);

    const diffDays = Math.ceil((expirationDate.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));

    if (diffDays < 0) {
        return `<span class="expiration-badge expiration-danger">Vencido - ${displayValue}</span>`;
    }

    if (diffDays <= 10) {
        return `<span class="expiration-badge expiration-warning">Por vencer - ${displayValue}</span>`;
    }

    return `<span class="expiration-badge expiration-ok">Vigente - ${displayValue}</span>`;
}

function deleteBatch(id) {
    rinconConfirm({
        title: 'Desactivar lote',
        text: 'El lote quedará inactivo y no se usará para stock disponible.',
        icon: 'question',
        confirmButtonText: 'Sí, desactivar',
        cancelButtonText: 'Cancelar'
    }).then(result => {
        if (!result.isConfirmed) {
            return;
        }

        $.ajax({
            url: `/Admin/ArticleBatches/Delete/${id}`,
            type: 'DELETE',
            success: function (data) {
                if (data.success) {
                    toastr.success(data.message);
                    $('#articleBatchesTable').DataTable().ajax.reload();
                } else {
                    toastr.error(data.message);
                }
            }
        });
    });
}
