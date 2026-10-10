// close any open dialog when its backdrop is clicked
// (::backdrop is not hit-testable, so backdrop clicks arrive with the dialog as event.target)
document.addEventListener('click', (event) => {
    document.querySelectorAll('dialog[open]').forEach(dialog => {
        if (event.target === dialog) {
            dialog.close();
        }
    });
});

htmx.onLoad( _ => {
    let sectionsContainer = document.querySelector('#sections');
    if (!sectionsContainer.dataset.sortable) {
        sectionsContainer.dataset.sortable = 'true';
        new Sortable(sectionsContainer, {
            handle: '.sortable-handler',
            animation: 150,
            group: 'sections'
        })    
    }
    
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