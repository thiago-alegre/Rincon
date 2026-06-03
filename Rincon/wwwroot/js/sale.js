$(document).ready(function () {
    const salesTable = $('#salesTable').DataTable({
        processing: true,
        serverSide: true,
        pageLength: 5,
        ajax: {
            url: '/Admin/Sales/GetAll',
            type: 'GET',
            datatype: 'json',
            data: function (data) {
                data.userId = $('#saleUserFilter').val();
                data.saleDate = $('#saleDateFilter').val();
            }
        },
        columns: [
            { data: 'date' },
            { data: 'paymentMethod' },
            {
                data: 'total',
                render: function (data) {
                    return '$ ' + data;
                }
            },
            { data: 'user' },
            {
                data: 'detailUrl',
                render: function (data) {
                    return `
                        <div class="datatable-action-group">
                            <a href="${data}" class="btn btn-soft-secondary btn-modern-sm">
                                <i class="fa fa-eye me-1"></i> Ver detalle
                            </a>
                        </div>
                    `;
                },
                orderable: false
            }
        ],
        language: {
            url: '//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json'
        },
        order: [[0, 'desc']]
    });

    $('#clearSalesFilters').on('click', function () {
        $('#saleUserFilter').val('');
        $('#saleDateFilter').val('');
        salesTable.ajax.reload();
    });

    $('#saleUserFilter, #saleDateFilter').on('change', function () {
        salesTable.ajax.reload();
    });
});
