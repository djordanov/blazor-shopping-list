// close any open dialog when its backdrop is clicked
// (::backdrop is not hit-testable, so backdrop clicks arrive with the dialog as event.target)
document.addEventListener('click', (event) => {
    document.querySelectorAll('dialog[open]').forEach(dialog => {
        if (event.target === dialog) {
            dialog.close();
        }
    });
});

htmx.onLoad( content => {
    let sections = document.querySelectorAll('.sortable')
    sections.forEach(section => {
        // skip already sortable sections
        if (section.dataset.sortable) {
            return;
        }
        section.dataset.sortable = 'true';
        
        new Sortable(section, {
            handle: '.sortable-handler',
            animation: 150,
            group: 'shared'
        })
    })
})