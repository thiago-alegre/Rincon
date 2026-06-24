$(document).ready(function () {
    $('#usersTable').DataTable({
        processing: true,
        serverSide: true,
        pageLength: 5,
        ajax: {
            url: '/Admin/Users/GetAll',
            type: 'GET',
            datatype: 'json'
        },
        columns: [
            {
                data: 'fullName',
                render: function (data) {
                    return `<span class="fw-semibold text-dark">${data || '-'}</span>`;
                }
            },
            {
                data: 'email',
                render: function (data) {
                    return `<span class="text-dark">${data || '-'}</span>`;
                }
            },
            {
                data: 'dni',
                render: function (data) {
                    return `<span class="text-dark">${data || '-'}</span>`;
                }
            },
            {
                data: 'phoneNumber',
                render: function (data) {
                    return `<span class="text-dark">${data || '-'}</span>`;
                }
            },
            {
                data: 'role',
                render: function (data) {
                    const roleText = getRoleDisplayName(data);

                    if (data === 'Admin') {
                        return `<span class="role-badge role-admin">${roleText}</span>`;
                    }

                    if (data === 'Employee') {
                        return `<span class="role-badge role-employee">${roleText}</span>`;
                    }

                    return `<span class="role-badge role-empty">${roleText}</span>`;
                }
            },
            {
                data: 'isActive',
                render: function (data) {
                    if (data) {
                        return `<span class="status-badge status-active">Activo</span>`;
                    }

                    return `<span class="status-badge status-blocked">Bloqueado</span>`;
                }
            },
            {
                data: null,
                render: function (data) {
                    const editButton = `
                        <a href="/Admin/Users/Upsert/${data.id}" class="btn btn-soft-secondary btn-modern-sm">
                            <i class="fa fa-pen me-1"></i> Editar
                        </a>
                    `;

                    if (!data.canToggleStatus) {
                        return `
                            <div class="datatable-action-group">
                                ${editButton}
                                <button type="button" class="btn btn-soft-secondary btn-modern-sm" disabled title="${data.statusProtectionReason || 'Usuario protegido'}">
                                    <i class="fa fa-shield-halved me-1"></i> Protegido
                                </button>
                            </div>
                        `;
                    }

                    const toggleText = data.isActive ? 'Bloquear' : 'Activar';
                    const toggleIcon = data.isActive ? 'fa-lock' : 'fa-unlock';
                    const toggleClass = data.isActive ? 'btn-soft-danger' : 'btn-soft-success';

                    return `
                        <div class="datatable-action-group">
                            ${editButton}
                            <button onclick="toggleUserStatus('${data.id}')" class="btn ${toggleClass} btn-modern-sm">
                                <i class="fa ${toggleIcon} me-1"></i> ${toggleText}
                            </button>
                        </div>
                    `;
                },
                orderable: false
            }
        ],
        language: {
            url: '//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json'
        }
    });
});

function getRoleDisplayName(role) {
    if (role === 'Admin') {
        return 'Administrador';
    }

    if (role === 'Employee') {
        return 'Empleado';
    }

    return role || 'Sin rol';
}

function toggleUserStatus(id) {
    rinconConfirm({
        title: '¿Cambiar estado del usuario?',
        text: 'El acceso del usuario será actualizado.',
        icon: 'question',
        confirmButtonText: 'Sí, continuar',
        cancelButtonText: 'Cancelar'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: '/Admin/Users/ToggleStatus',
                type: 'POST',
                data: { id: id },
                success: function (response) {
                    if (response.success) {
                        toastr.success(response.message);
                        $('#usersTable').DataTable().ajax.reload();
                    } else {
                        toastr.error(response.message);
                    }
                },
                error: function () {
                    toastr.error('Ocurrió un error al actualizar el usuario');
                }
            });
        }
    });
}
