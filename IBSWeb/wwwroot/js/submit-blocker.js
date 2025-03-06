$(document).ready(function () {
    let isSubmitting = sessionStorage.getItem('isSubmitting') === 'true';

    if (isSubmitting) {
        $('button[type="submit"]').prop('disabled', true).html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Processing...');
    }

    $('form').on('submit', function () {
        $(this).validate();
        if (!$(this).valid()) {
            return false; // Stop execution if invalid
        }

        const submitButton = $(this).find('button[type="submit"]');

        isSubmitting = true;
        sessionStorage.setItem('isSubmitting', 'true');
        submitButton.prop('disabled', true).html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Processing...');
    });

    $(window).on('beforeunload', function () {
        sessionStorage.removeItem('isSubmitting');
    });
});
