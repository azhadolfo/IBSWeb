$(function () {
    const root = document.getElementById('collection-tagging');
    if (!root) return;
    const categories = JSON.parse(document.getElementById('collection-categories').textContent);
    const categorySelect = document.getElementById('CollectionCategoryId');
    const typeSelect = document.getElementById('TagType');
    const recordSelect = document.getElementById('TagId');
    const payerName = document.getElementById('PayerName');
    const payerAddress = document.getElementById('PayerAddress');
    const error = document.getElementById('tag-lookup-error');
    let requestVersion = 0;
    const selectedCategory = () => categories.find(c => String(c.Id) === categorySelect.value);

    function updateVisibility() {
        const category = selectedCategory();
        const tagging = category && category.TaggingRequirement !== 0;
        const tagged = tagging && typeSelect.value !== '';
        const manual = !tagged || typeSelect.value === '3';
        document.getElementById('tag-type-group').hidden = !tagging;
        typeSelect.disabled = !tagging;
        typeSelect.required = tagging && category.TaggingRequirement === 2;
        document.getElementById('tag-record-group').hidden = !tagged;
        recordSelect.disabled = !tagged;
        recordSelect.required = Boolean(tagged);
        document.getElementById('manual-payer').hidden = !manual;
        document.getElementById('derived-payer').hidden = manual;
        payerName.disabled = payerAddress.disabled = !manual;
        payerName.required = manual;
        root.closest('form').querySelectorAll('[type="submit"]').forEach(button => {
            button.disabled = !category;
        });
    }

    async function loadRecords() {
        const version = ++requestVersion;
        recordSelect.replaceChildren(new Option('Select a record', ''));
        $(recordSelect).trigger('change');
        error.textContent = '';
        updateVisibility();
        if (!typeSelect.value) return;
        const url = new URL(root.dataset.optionsUrl, window.location.origin);
        url.searchParams.set('categoryId', categorySelect.value);
        url.searchParams.set('tagType', typeSelect.value);
        if (root.dataset.receiptId) url.searchParams.set('receiptId', root.dataset.receiptId);
        try {
            const response = await fetch(url, { headers: { Accept: 'application/json' } });
            if (!response.ok || response.redirected) throw new Error('Lookup failed');
            const options = await response.json();
            if (version !== requestVersion) return;
            for (const option of options) recordSelect.add(new Option(option.text, option.value));
            $(recordSelect).trigger('change');
        } catch {
            if (version === requestVersion) error.textContent = 'Could not load master-file records. Select the type again or reload the page to retry.';
        }
    }

    function updateTypes(initial) {
        const category = selectedCategory();
        const previous = initial ? typeSelect.dataset.selected : '';
        typeSelect.replaceChildren(new Option(category?.TaggingRequirement === 2 ? 'Select a type' : 'No tagging', ''));
        if (category && category.TaggingRequirement !== 0) {
            if (category.AllowCompany) typeSelect.add(new Option('Company', '1'));
            if (category.AllowEmployee) typeSelect.add(new Option('Employee', '2'));
            if (category.AllowBankAccount) typeSelect.add(new Option('Bank Account', '3'));
        }
        typeSelect.value = previous || '';
        if (!initial && typeSelect.options.length === 2) typeSelect.selectedIndex = 1;
        $(typeSelect).trigger('change.select2');
        if (!initial) {
            payerName.value = payerAddress.value = '';
            loadRecords();
        }
        updateVisibility();
    }
    $(categorySelect).on('change', () => updateTypes(false));
    $(typeSelect).on('change', () => {
        payerName.value = payerAddress.value = '';
        loadRecords();
    });
    updateTypes(true);
});
