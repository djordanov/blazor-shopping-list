const shoppingListId = document.querySelector('#sections').getAttribute('data-shopping-list-id');

// close any open dialog when its backdrop is clicked
// (::backdrop is not hit-testable, so backdrop clicks arrive with the dialog as event.target)
document.addEventListener('click', (event) => {
    document.querySelectorAll('dialog[open]').forEach(dialog => {
        if (event.target === dialog) {
            dialog.close();
        }
    });
});

// click handlers for item count stepper buttons
const steppers = document.querySelectorAll('.item-count-stepper');
steppers.forEach(stepper => {
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
            stepper.querySelector('span').innerText = item['count'];
            decrementButton.toggleAttribute('disabled', item['count'] <= 0)
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
            stepper.querySelector('span').innerText = item['count'];
            decrementButton.toggleAttribute('disabled', item['count'] <= 0)
        } else {
            console.error('item increment failed', response.status);
        }
    })
})

// click handlers for item count status buttons
const itemStatusButtons = document.querySelectorAll('.item-status-button');
itemStatusButtons.forEach(button => {
    const itemId = button.closest('.item-card').getAttribute('data-item-id');
    
    button.addEventListener('click', async () => {
        const currentStatus = button.getAttribute('data-item-status');
        let icon, nextStatus, ariaLabel;
        switch (currentStatus) {
            case 'Noted':
                icon = 'ti ti-square-rounded-minus';
                nextStatus = 'Planned';
                ariaLabel = 'Mark item as planned';
                break;
            case 'Planned':
                icon = 'ti ti-square-rounded-check';
                nextStatus = 'Collected';
                ariaLabel = 'Mark item as collected';
                break;
            case 'Collected':
                icon = 'ti ti-square-rounded';
                nextStatus = 'Noted';
                ariaLabel = 'Mark item as noted';
                break;
            default:
                console.error('unexpected item status', currentStatus);
                return;
        }
    
        const response = await fetch(`/api/shopping-list/${shoppingListId}/item/${itemId}`, {
            method: 'patch',
            headers: {'content-type': 'application/json'},
            body: JSON.stringify({ 'ItemStatus': nextStatus})
        });
        if (response.ok) {
            button.setAttribute('data-item-status', nextStatus);
            button.setAttribute('aria-label', ariaLabel);
            button.querySelector('i').className = icon;
        } else {
            console.error('item status patch failed', response.status);
        }});
});

// sorting sections and items on drag and drop
const sectionsContainer = document.querySelector('#sections');
if (sectionsContainer) {
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
