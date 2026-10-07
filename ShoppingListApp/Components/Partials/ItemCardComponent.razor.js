let sections = document.querySelectorAll('.section > div')
sections.forEach(section => {
    new Sortable(section, {
        handle: '.handle',
        animation: 150,
        ghostClass: 'blue-background-class'
    })
})