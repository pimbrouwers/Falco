const bulletEditor = document.getElementById('bullet-editor')
const bulletEditorHtml = document.getElementById('bullet-editor-html')
const bulletEditorText = document.getElementById('bullet-editor-text')

window.onload = () => {
    if (bulletEditor && bulletEditorHtml && bulletEditorText) {
        bulletEditorHtml.value = bulletEditor.innerHTML
        bulletEditorText.value = bulletEditor.innerText

        bulletEditor.focus()

        bulletEditor.addEventListener('input', (e) => {
            bulletEditorHtml.value = e.srcElement.innerHTML.trim()
            bulletEditorText.value = e.srcElement.innerText.trim()
        })
    }
}