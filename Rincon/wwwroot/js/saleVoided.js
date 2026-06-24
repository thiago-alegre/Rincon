$(document).ready(function () {
    const voidedSalesTable = $('#voidedSalesTable').DataTable({
        processing: true,
        serverSide: true,
        pageLength: 5,
        ajax: {
            url: '/Admin/Sales/GetVoided',
            type: 'GET',
            datatype: 'json',
            data: function (data) {
                data.userId = $('#voidedSaleUserFilter').val();
                data.saleDate = $('#voidedSaleDateFilter').val();
            }
        },
        columns: [
            { data: 'date' },
            { data: 'movementDate' },
            { data: 'paymentMethod' },
            {
                data: 'total',
                render: function (data) {
                    return '$ ' + data;
                }
            },
            { data: 'user' },
            {
                data: 'status',
                render: function (data, type, row) {
                    return `<span class="status-badge ${row.statusClass}">${data}</span>`;
                }
            },
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
        order: [[1, 'desc']]
    });

    $('#clearVoidedSalesFilters').on('click', function () {
        $('#voidedSaleUserFilter').val('');
        $('#voidedSaleDateFilter').val('');
        voidedSalesTable.ajax.reload();
    });

    $('#voidedSaleUserFilter, #voidedSaleDateFilter').on('change', function () {
        voidedSalesTable.ajax.reload();
    });
});
