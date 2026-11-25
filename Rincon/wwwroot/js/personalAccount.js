$(document).ready(function () {
    $('#personalAccountsTable').DataTable({
        processing: true,
        serverSide: true,
        pageLength: 5,
        ajax: {
            url: '/Employee/PersonalAccounts/GetAll',
            type: 'GET',
            datatype: 'json'
        },
        columns: [
            {
                data: 'fullName',
                render: function (data, type) {
                    if (type !== 'display') {
                        return data;
                    }

                    return escapePersonalAccountHtml(data || '-');
                }
            },
            { data: 'dni' },
            { data: 'phone', render: data => data || '-' },
            {
                data: 'address',
                render: function (data, type) {
                    if (type !== 'display') {
                        return data;
                    }

                    const fullAddress = data || '-';
                    const displayAddress = fullAddress.length > 20
                        ? `${fullAddress.substring(0, 20)}...`
                        : fullAddress;

                    return `<span class="personal-account-address" title="${escapePersonalAccountHtml(fullAddress)}">${escapePersonalAccountHtml(displayAddress)}</span>`;
                }
            },
            {
                data: 'debt',
                render: function (data, type, row) {
                    const debtClass = row.debtValue > 0 ? 'text-danger' : 'text-success';
                    return `<span class="fw-bold ${debtClass}">$ ${data}</span>`;
                }
            },
            { data: 'debtSince' },
            {
                data: null,
                render: function (data) {
                    let buttons = `
                        <div class="datatable-action-group justify-content-end">
                            <a href="${data.detailUrl}" class="btn btn-soft-secondary btn-modern-sm personal-account-action-btn" title="Ver detalle" aria-label="Ver detalle">
                                <i class="fa fa-eye" aria-hidden="true"></i>
                            </a>
                    `;

                    if (data.canManage) {
                        buttons += `
                            <a href="${data.editUrl}" class="btn btn-soft-secondary btn-modern-sm personal-account-action-btn" title="Editar" aria-label="Editar">
                                <i class="fa fa-pen" aria-hidden="true"></i>
                            </a>
                            <button type="button" class="btn btn-soft-danger btn-modern-sm personal-account-action-btn" onclick="deletePersonalAccount(${data.id})" title="Eliminar" aria-label="Eliminar">
                                <i class="fa fa-trash" aria-hidden="true"></i>
                            </button>
                        `;
                    }

                    buttons += '</div>';
                    return buttons;
                },
                orderable: false,
                className: 'text-end text-nowrap'
            }
        ],
        autoWidth: false,
        columnDefs: [
            { targets: 0, width: '180px' },
            { targets: 1, width: '125px' },
            { targets: 2, width: '145px' },
            { targets: 3, width: '280px' },
            { targets: 4, width: '120px' },
            { targets: 5, width: '115px' },
            { targets: 6, width: '140px' }
        ],
        language: {
            url: '//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json'
        }
    });
});

function escapePersonalAccountHtml(value) {
    return String(value ?? '')
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#039;');
}

function deletePersonalAccount(id) {
    rinconConfirm({
        title: '¿Eliminar cuenta personal?',
        text: 'La cuenta se desactivará si no tiene deuda pendiente.',
        icon: 'warning',
        confirmButtonText: 'Sí, eliminar',
        cancelButtonText: 'Cancelar'
    }).then(result => {
        if (!result.isConfirmed) {
            return;
        }

        $.ajax({
            url: `/Employee/PersonalAccounts/Delete/${id}`,
            type: 'DELETE',
            success: function (data) {
                if (data.success) {
                    toastr.success(data.message);
                    $('#personalAccountsTable').DataTable().ajax.reload();
                } else {
                    toastr.error(data.message);
                }
            }
        });
    });
}
