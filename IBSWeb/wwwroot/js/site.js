// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(document).ready(function () {
    $('.js-select2').select2({
        placeholder: "Select an option...",
        allowClear: true,
        width: 'resolve',
        theme: 'classic'
    });
});

$(document).ready(function () {
    $('.js-multiple').select2({
        placeholder: "Select an option",
        allowClear: true,
        width: 'resolve'
    });
});

// hack to fix jquery 3.6 focus security patch that bugs auto search in select-2
$(document).on('select2:open', (e) => {
    let searchField = document.querySelector('.select2-container--open .select2-search__field');
    if (searchField) {
        searchField.focus();
    }
});


var table;

$(document).ready(function () {
    table = $('#dataTable').DataTable({
        stateSave: true,
        processing: true, // Enable the processing indicator
    });
});

function validateDate() {
    let dateFrom = document.getElementById("dateFrom").value;
    let dateTo = document.getElementById("dateTo").value;
    if (dateFrom > dateTo) {
        alert("Date From must be less than or equal to Date To");
        return false;
    }
    return true;
}

function formatNumber(number) {
    return number.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function parseNumber(formattedNum) {
    return parseFloat(formattedNum.replace(/,/g, '')) || 0;
}

// Dynamic date to in books
document.addEventListener('DOMContentLoaded', function () {
    var dateFromInput = document.getElementById('DateFrom');
    var dateToInput = document.getElementById('DateTo');

    // Add an event listener to DateFrom input
    dateFromInput?.addEventListener('change', function () {
        // Set DateTo input value to DateFrom input value
        dateToInput.value = dateFromInput.value;
    });
});

// Format the tin number
document.addEventListener('DOMContentLoaded', () => {
    const inputFields = document.querySelectorAll('.formattedTinNumberInput');

    inputFields.forEach(inputField => {
        inputField.addEventListener('input', (e) => {
            let value = e.target.value.replace(/-/g, ''); // Remove existing dashes
            let formattedValue = '';

            // Add dashes after every 3 digits, keeping the last 5 digits without dashes
            for (let i = 0; i < value.length; i++) {
                if (i === 3 || i === 6 || i === 9) {
                    formattedValue += '-';
                }
                formattedValue += value[i];
            }

            // If there are more than 12 characters, don't add a dash after the 10th character (i.e., for the last 5 digits)
            if (formattedValue.length > 12) {
                formattedValue = formattedValue.substring(0, 12) + formattedValue.substring(12).replace(/-/g, '');
            }

            e.target.value = formattedValue;
        });

        inputField.addEventListener('keydown', (e) => {
            if (e.key === 'Backspace') {
                let value = e.target.value;
                // Remove the dash when backspace is pressed if it is at the end of a section of 3 digits
                if (value.endsWith('-')) {
                    e.target.value = value.slice(0, -1);
                }
            }
        });
    });
});

//navigation bar dropend implementation
document.addEventListener("DOMContentLoaded", function () {
    // Get all dropend elements
    const dropends = document.querySelectorAll(".dropend");

    // Track the currently open parent dropend
    let openParentDropend = null;

    dropends.forEach(function (dropend) {
        dropend.addEventListener("click", function (event) {
            // Stop event from bubbling up
            event.stopPropagation();

            const clickedMenu = this.querySelector(".dropdown-menu");

            // If clicking on a child menu inside an open parent, allow it
            if (openParentDropend && openParentDropend.contains(this)) {
                return;
            }

            // Close the currently open parent dropend if different
            if (openParentDropend && openParentDropend !== this) {
                const openMenu = openParentDropend.querySelector(".dropdown-menu");
                if (openMenu) {
                    openMenu.classList.remove("show");
                }
            }

            // Open the clicked dropend
            if (clickedMenu) {
                clickedMenu.classList.add("show");
                openParentDropend = this;
            }
        });
    });
});
