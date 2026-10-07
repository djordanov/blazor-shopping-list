htmx.onLoad( content => {
    let sections = document.querySelectorAll('.sortable')
    sections.forEach(section => {
        // skip already sortable sections
        if (section.dataset.sortable) {
            return;
        }
        section.dataset.sortable = 'true';
        
        new Sortable(section, {
            handle: '.handle',
            animation: 150,
            group: 'shared'
        })
    })  
})