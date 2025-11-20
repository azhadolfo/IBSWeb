class MasterFileSelector {
    constructor(urls) {
        this.urls = urls;
        this.masterFileTypes = {
            BANK: {
                id: 'bank',
                title: 'Bank Account',
                url: urls.getBankAccounts,
                triggerAccounts: ['101010400 Fund Transfer', '101010100 Cash in Bank'],
                placeholder: 'Select a bank account',
                formatOption: (item) => `${item.accountName} - ${item.accountNumber}`,
                inputName: 'BankMasterFileId'
            },
            COMPANY: {
                id: 'company',
                title: 'Company',
                url: urls.getCompanies,
                triggerAccount: '101020600 AR-Exchange Check',
                placeholder: 'Select a company',
                formatOption: (item) => `${item.accountNumber} - ${item.accountName}`,
                inputName: 'CompanyMasterFileId'
            },
            EMPLOYEE: {
                id: 'employee',
                title: 'Employee',
                url: urls.getEmployees,
                triggerAccount: '101020400 Advances from Officers and Employees',
                placeholder: 'Select an employee',
                formatOption: (item) => `${item.accountNumber} - ${item.accountName}`,
                inputName: 'EmployeeMasterFileId'
            },
            ///TODO change to actual trigger account
            CUSTOMER: {
                id: 'customer',
                title: 'Customer',
                url: urls.getCustomers,
                triggerAccount: '101020500 AR-Non Trade Receivable',
                placeholder: 'Select a customer',
                formatOption: (item) => `${item.accountNumber} - ${item.accountName}`,
                inputName: 'CustomerMasterFileId'
            },
            SUPPLIER: {
                id: 'supplier',
                title: 'Supplier',
                url: urls.getSuppliers,
                triggerAccount: '101020500 AR-Non Trade Receivable',
                placeholder: 'Select a supplier',
                formatOption: (item) => `${item.accountNumber} - ${item.accountName}`,
                inputName: 'SupplierMasterFileId'
            },
        };

        this.initializeEventListeners();
    }

    initializeEventListeners() {
        $(document).on('change', '.chart-of-accounts', (e) => {
            const selectedAccount = $(e.target).val();
            const row = $(e.target).closest('tr');

            // Reset amount
            row.find('.amount').val(0);

            // Clear all existing master file IDs and restore original account text
            this.clearAllMasterFileIds(row);

            // Handle the new account selection
            this.handleAccountChange(selectedAccount, row);

            if (typeof recalculateAllRows === 'function') {
                recalculateAllRows();
            }
        });
    }

    clearAllMasterFileIds(row) {
        const index = row.index();
        const accountSelect = row.find('.chart-of-accounts');

        // Restore original text if it exists
        if (accountSelect.data('original-text')) {
            accountSelect.find('option:selected').text(accountSelect.data('original-text'));
            accountSelect.removeData('original-text');
        }

        // Remove all master file ID inputs
        Object.values(this.masterFileTypes).forEach(type => {
            const inputName = `AccountingEntries[${index}].${type.inputName}`;
            row.find(`input[name="${inputName}"]`).remove();
        });
    }

    handleAccountChange(selectedAccount, row) {
        let matchFound = false;

        // Special handling for AR - Non Trade Receivable
        if (selectedAccount === '101020500 AR-Non Trade Receivable') {
            this.showMasterFileTypeSelector(row);
            matchFound = true;
        } else {
            Object.values(this.masterFileTypes).forEach(type => {
                const triggers = type.triggerAccounts || [type.triggerAccount];
                if (triggers.includes(selectedAccount)) {
                    this.showMasterFileModal(type, row);
                    matchFound = true;
                }
            });
        }

        // If no matching master file type is found, ensure the row is clean
        if (!matchFound) {
            this.clearAllMasterFileIds(row);
        }
    }

    showMasterFileTypeSelector(row) {
        const modalHTML = `
            <div class="modal fade" id="masterFileTypeModal" tabindex="-1" data-bs-backdrop="static">
                <div class="modal-dialog">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title">Select Master File Type</h5>
                        </div>
                        <div class="modal-body">
                            <div class="form-group">
                                <label class="form-label">Choose Master File Type</label>
                                <select id="masterFileTypeSelect" class="form-select">
                                    <option value="">Select type...</option>
                                    <option value="BANK">Bank Account</option>
                                    <option value="COMPANY">Company</option>
                                    <option value="EMPLOYEE">Employee</option>
                                    <option value="CUSTOMER">Customer</option>
                                    <option value="SUPPLIER">Supplier</option>
                                </select>
                            </div>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-primary" id="confirmMasterFileType">
                                Continue
                            </button>
                        </div>
                    </div>
                </div>
            </div>`;

        // Remove existing modal if any
        $('#masterFileTypeModal').remove();

        // Add new modal
        $('body').append(modalHTML);

        const modal = new bootstrap.Modal(document.getElementById('masterFileTypeModal'), {
            backdrop: 'static',
            keyboard: false
        });

        // Handle type selection
        $('#confirmMasterFileType').on('click', () => {
            const selectedType = $('#masterFileTypeSelect').val();
            if (!selectedType) {
                Swal.fire({
                    title: 'Required',
                    text: 'Please select a master file type',
                    icon: 'warning'
                });
                return;
            }

            modal.hide();
            this.showMasterFileModal(this.masterFileTypes[selectedType], row);
        });

        modal.show();
    }

    createModal(type) {
        const modalId = `${type.id}Modal`;

        // Destroy Select2 before removing modal
        const existingSelect = $(`#${type.id}Select`);
        if (existingSelect.length && existingSelect.data('select2')) {
            existingSelect.select2('destroy');
        }

        $(`#${modalId}`).remove();

        const modalHTML = `
        <div class="modal fade" id="${modalId}" tabindex="-1" style="z-index: 1060;">
            <div class="modal-dialog">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">Select ${type.title}</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        <div class="form-group">
                            <label for="${type.id}Select" class="form-label">${type.title}</label>
                            <select id="${type.id}Select" class="form-select" style="width:100%">
                                <option value="">Loading...</option>
                            </select>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                        <button type="button" class="btn btn-primary select-master-file" data-type="${type.id}">
                            Select
                        </button>
                    </div>
                </div>
            </div>
        </div>`;

        $('body').append(modalHTML);

        // DO NOT initialize Select2 here - it will be done in showMasterFileModal

        // Handle modal cleanup on close
        $(`#${modalId}`).on('hidden.bs.modal', () => {
            const select = $(`#${type.id}Select`);
            if (select.data('select2')) {
                select.select2('destroy');
            }
        });

        // Handle cancel button
        $(`#${modalId}`).on('hidden.bs.modal', () => {
            const row = $(`#${modalId}`).data('row');
            const accountSelect = row.find('.chart-of-accounts');

            if (!row.find(`input[name$="${type.inputName}"]`).length) {
                accountSelect.val('').trigger('change');
            }
        });
    }

    async showMasterFileModal(type, row) {
        this.createModal(type);
        const modalId = `${type.id}Modal`;
        const modal = new bootstrap.Modal(document.getElementById(modalId));

        try {
            const response = await $.ajax({
                url: type.url,
                type: 'GET',
                dataType: 'json'
            });

            const select = $(`#${type.id}Select`);

            // DESTROY existing Select2 first
            if (select.data('select2')) {
                select.select2('destroy');
            }

            select.empty();

            if (response && response.length > 0) {
                select.append(`<option value="">Select ${type.title.toLowerCase()}...</option>`);
                response.forEach(item => {
                    select.append(`<option value="${item.id}">${type.formatOption(item)}</option>`);
                });
            } else {
                select.append('<option value="">No records found</option>');
            }

            // Initialize Select2 ONLY ONCE here
            select.select2({
                dropdownParent: $(`#${modalId}`),
                placeholder: type.placeholder,
                width: '100%',
                theme: 'classic',
                dropdownCssClass: 'select2-dropdown-above-modal'
            });

            $(`#${modalId}`).data('row', row);
            modal.show();

        } catch (error) {
            console.error(`Error loading ${type.title} data:`, error);
            Swal.fire({
                title: 'Error',
                text: `Failed to load ${type.title.toLowerCase()} data. Please try again.`,
                icon: 'error'
            });

            row.find('.chart-of-accounts').val('').trigger('change');
        }
    }

    // In masterFileSelector.js, update handleSelection
    handleSelection(type, row, selectedId, selectedText) {
        if (!selectedId) {
            Swal.fire({
                title: 'Required',
                text: `Please select a ${type.title.toLowerCase()}`,
                icon: 'warning'
            });
            return;
        }

        const index = row.index();
        const inputName = `AccountingEntries[${index}].${type.inputName}`;

        this.clearAllMasterFileIds(row);

        row.find(`input[name$="${type.inputName}"]`).remove();
        row.append(`<input type="hidden" name="${inputName}" value="${selectedId}">`);

        const accountSelect = row.find('.chart-of-accounts');
        accountSelect.find('option:selected').text(
            `${accountSelect.find('option:selected').text().split('(')[0].trim()} (${selectedText})`
        );

        // Properly destroy and close modal
        const modalElement = document.getElementById(`${type.id}Modal`);
        const select = $(`#${type.id}Select`);

        if (select.data('select2')) {
            select.select2('destroy');
        }

        const modalInstance = bootstrap.Modal.getInstance(modalElement);
        if (modalInstance) {
            modalInstance.hide();
        }
    }
}