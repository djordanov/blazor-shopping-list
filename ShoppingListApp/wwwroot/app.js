// close any open dialog when its backdrop is clicked
// (::backdrop is not hit-testable, so backdrop clicks arrive with the dialog as event.target)
document.addEventListener('click', (event) => {
    document.querySelectorAll('dialog[open]').forEach(dialog => {
        if (event.target === dialog) {
            dialog.close();
        }
    });
});

let sectionsContainer = document.querySelector('#sections');
if (sectionsContainer)
{
    const shoppingListId = sectionsContainer.getAttribute('data-shopping-list-id')
    if (!sectionsContainer.dataset.sortable) {
        sectionsContainer.dataset.sortable = 'true';
        new Sortable(sectionsContainer, {
            handle: '.sortable-handler',
            animation: 150,
            group: 'sections',
            onEnd: async () => {
                let sections = Array.from(sectionsContainer.querySelectorAll('.section'));
                let sectionIds = sections.map(s => s.getAttribute('data-section-id'));
                let response = await fetch(`/api/shopping-list/${shoppingListId}/sort`, {
                    method: 'post',
                    headers: new Headers({'content-type': 'application/json'}),
                    body: JSON.stringify(sectionIds)
                });
                if (!response.ok)
                {
                    console.error('section sort failed', response.status);
                }
            }
        })
    }
}
    
htmx.onLoad( _ => {    
    let itemsContainers = document.querySelectorAll('.items')
    itemsContainers.forEach(itemsContainer => {
        // skip already sortable sections
        if (itemsContainer.dataset.sortable) {
            return;
        }
        itemsContainer.dataset.sortable = 'true';
        
        new Sortable(itemsContainer, {
            handle: '.sortable-handler',
            animation: 150,
            group: 'items'
        })
    })
})