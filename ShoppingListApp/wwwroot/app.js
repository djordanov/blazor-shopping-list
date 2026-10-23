// close any open dialog when its backdrop is clicked
// (::backdrop is not hit-testable, so backdrop clicks arrive with the dialog as event.target)
document.addEventListener('click', (event) => {
    document.querySelectorAll('dialog[open]').forEach(dialog => {
        if (event.target === dialog) {
            dialog.close();
        }
    });
});

const sectionsContainer = document.querySelector('#sections');
if (sectionsContainer)
{
    const shoppingListId = sectionsContainer.getAttribute('data-shopping-list-id');
    
    // sorting sections and items on drag and drop
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
