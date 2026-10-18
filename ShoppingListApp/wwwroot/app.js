// close any open dialog when its backdrop is clicked
// (::backdrop is not hit-testable, so backdrop clicks arrive with the dialog as event.target)
document.addEventListener('click', (event) => {
    document.querySelectorAll('dialog[open]').forEach(dialog => {
        if (event.target === dialog) {
            dialog.close();
        }
    });
});

// click handlers for item buttons
const steppers = document.querySelectorAll('.item-count-stepper');
steppers.forEach(stepper => {
    const shoppingListId = stepper.closest('#sections').getAttribute('data-shopping-list-id');
    const itemId = stepper.closest('.item-card').getAttribute('data-item-id');

    const decrementButton = stepper.querySelector('.decrement');
    decrementButton.addEventListener('click', async () => {
        const response = await fetch(`/api/shopping-list/${shoppingListId}/item/${itemId}`, {
            method: 'patch',
            headers: {'content-type': 'application/json'},
            body: JSON.stringify({ 'CountDelta': -1})
        });
        if (response.ok) {
            const item = await response.json();
            stepper.querySelector('span').innerHTML = item['count'];
            decrementButton.toggleAttribute('disabled', parseInt(item['count']) <= 0)
        } else {
            console.error('item decrement failed', response.status);
        }
    })

    const incrementButton = stepper.querySelector('.increment');
    incrementButton.addEventListener('click', async () => {
        const response = await fetch(`/api/shopping-list/${shoppingListId}/item/${itemId}`, {
            method: 'patch',
            headers: {'content-type': 'application/json'},
            body: JSON.stringify({ 'CountDelta': +1})
        });
        if (response.ok) {
            const item = await response.json();
            stepper.querySelector('span').innerHTML = item['count'];
            decrementButton.toggleAttribute('disabled', parseInt(item['count']) <= 0)
        } else {
            console.error('item increment failed', response.status);
        }
    })
})

// sorting sections and items on drag and drop
const sectionsContainer = document.querySelector('#sections');
if (sectionsContainer) {
    const shoppingListId = sectionsContainer.getAttribute('data-shopping-list-id')
    if (!sectionsContainer.dataset.sortable) {
        sectionsContainer.dataset.sortable = 'true';
        new Sortable(sectionsContainer, {
            handle: '.sortable-handler',
            animation: 150,
            group: 'sections',
            onEnd: async () => {
                let sectionIds = [...sectionsContainer.querySelectorAll('.section')]
                    .map(s => s.getAttribute('data-section-id'));
                postIds(`/api/shopping-list/${shoppingListId}/sort`, sectionIds, 'section sort failed');
            }
        })
    }

    let itemsContainers = document.querySelectorAll('.items')
    itemsContainers.forEach(itemsContainer => {
        // skip already sortable sections
        if (itemsContainer.dataset.sortable) {
            return;
        }
        itemsContainer.dataset.sortable = 'true';

        let sectionId = itemsContainer.closest('.section').getAttribute('data-section-id');
        new Sortable(itemsContainer, {
            handle: '.sortable-handler',
            animation: 150,
            group: 'items',
            onAdd: () => {
                let itemIds = [...itemsContainer.querySelectorAll('.item-card')]
                    .map(i => i.getAttribute('data-item-id'));
                postIds(`/api/shopping-list/${shoppingListId}/section/${sectionId}/sort`, itemIds, 'item sort failed');
            },
            onUpdate: () => {
                let itemIds = [...itemsContainer.querySelectorAll('.item-card')]
                    .map(i => i.getAttribute('data-item-id'));
                postIds(`/api/shopping-list/${shoppingListId}/section/${sectionId}/sort`, itemIds, 'item sort failed');
            }
        })
    })
}

async function postIds(url, ids, errorMessage) {
    let response = await fetch(url, {
        method: 'post',
        headers: {'content-type': 'application/json'},
        body: JSON.stringify(ids)
    });
    if (!response.ok) {
        console.error(errorMessage, response.status);
    }
}
