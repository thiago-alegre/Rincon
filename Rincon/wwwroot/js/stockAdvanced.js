$(document).ready(function () {
    const articleSearch = $('#batchArticleSearch');
    const goToBatchArticleButton = $('#goToBatchArticle');

    if (articleSearch.length) {
        articleSearch.select2({
            theme: 'bootstrap-5',
            width: '100%',
            placeholder: articleSearch.data('placeholder') || 'Buscá un artículo',
            allowClear: true,
            minimumInputLength: 0,
            ajax: {
                url: '/Admin/Stock/SearchBatchArticles',
                dataType: 'json',
                delay: 200,
                data: function (params) {
                    return {
                        term: params.term || '',
                        page: params.page || 1
                    };
                },
                processResults: function (data) {
                    return data;
                }
            },
            templateResult: formatArticleResult,
            templateSelection: function (item) {
                return item.text || 'Buscá un artículo';
            },
            language: {
                inputTooShort: function () {
                    return 'Escribí para buscar';
                },
                searching: function () {
                    return 'Buscando...';
                },
                loadingMore: function () {
                    return 'Cargando más...';
                },
                noResults: function () {
                    return 'Sin resultados';
                }
            }
        });

        articleSearch.on('change', function () {
            goToBatchArticleButton.prop('disabled', !articleSearch.val());
        });
    }

    goToBatchArticleButton.on('click', function () {
        const articleId = articleSearch.val();

        if (!articleId) {
            return;
        }

        window.location.href = `/Admin/ArticleBatches/Index?articleId=${articleId}`;
    });

    $('#stockAdvancedTable').DataTable({
        serverSide: true,
        processing: true,
        pageLength: 5,
        ajax: {
            url: '/Admin/Stock/GetAll',
            type: 'GET',
            datatype: 'json'
        },
        columns: [
            { data: 'product' },
            { data: 'category' },
            {
                data: 'stock',
                render: function (data, type, row) {
                    return `<span class="stock-badge ${getStatusClass(row.status)}">${formatStock(data, row)}</span>`;
                }
            },
            {
                data: 'stockMin',
                render: function (data, type, row) {
                    return formatStock(data, row);
                }
            },
            {
                data: 'batchCount',
                render: function (data) {
                    return data > 0
                        ? `<span class="status-badge status-active">${data}</span>`
                        : '<span class="status-badge expiration-empty">Sin lotes</span>';
                }
            },
            {
                data: 'expirationDate',
                render: function (data, type, row) {
                    return renderBatchExpiration(data, row.expirationDisplay);
                }
            },
            {
                data: 'statusDisplay',
                orderable: false,
                render: function (data, type, row) {
                    return `<span class="status-badge ${getStatusClass(row.status)}">${data}</span>`;
                }
            },
            {
                data: 'detailUrl',
                orderable: false,
                searchable: false,
                render: function (data) {
                    return `
                        <div class="datatable-action-group justify-content-end">
                            <a href="${data}" class="btn btn-soft-box btn-modern-sm" title="Ver lotes del artículo">
                                <i class="bi bi-boxes me-1"></i> Lotes
                            </a>
                        </div>
                    `;
                }
            }
        ],
        language: {
            url: '//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json'
        },
        order: [[0, 'asc']]
    });
});

function formatArticleResult(item) {
    if (!item.id) {
        return item.text;
    }

    const category = item.category ? `<small class="d-block text-muted">${item.category}</small>` : '';
    const mode = item.usesBatches
        ? '<small class="status-badge status-active mt-1">Usa lotes</small>'
        : '<small class="status-badge expiration-empty mt-1">Sin lotes aún</small>';

    return $(`
        <div>
            <strong>${item.text}</strong>
            ${category}
            ${mode}
        </div>
    `);
}

function formatStock(value, row) {
    const number = Number(value);
    const formatted = Number.isInteger(number)
        ? number.toLocaleString('es-AR', { maximumFractionDigits: 0 })
        : number.toLocaleString('es-AR', { minimumFractionDigits: 0, maximumFractionDigits: 3 });

    if (!row.isSoldByWeight || row.unitOfMeasure === 'Unidad') {
        return `${formatted} u`;
    }

    return `${formatted} kg`;
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

function getStatusClass(status) {
    if (status === 'danger') {
        return 'expiration-danger';
    }

    if (status === 'warning') {
        return 'expiration-warning';
    }

    if (status === 'ok') {
        return 'stock-ok';
    }

    return 'expiration-empty';
}
