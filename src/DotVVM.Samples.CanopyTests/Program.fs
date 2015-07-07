open canopy
open runner
open types
open System
  
let dotvvmath = "http://localhost:8628"


"Redirect Sample (10)" &&& fun _ ->
    url (dotvvmath + "/Sample10")
    let button = "input[type='button']"

    on "/Sample10?time="
    let url1 = currentUrl()

    click button
    on "/Sample10?time="
    if url1 = currentUrl() then raise (CanopyException("not redirected"))

"Validation Sample (11)" &&& fun _ ->
    url (dotvvmath + "/Sample11")
    let button = "input[type='button']"
    let input = "input[type='text']"
    let checkNotValid() =
        displayed "#hideWhenValid"
        displayed "#addCssClass.validator"
        displayed "#displayErrorMessage"
        displayed "#validationSummary li"

    click button
    checkNotValid()
    input << "not valid email"
    click button
    checkNotValid()
    input << "email@addre.ss"
    click button
    count "table.table tr" 4

"SPA Sample (17)" &&& fun _ ->
    url (dotvvmath + "/Sample17/14")

    click "input[value='Increase']"
    "#value span" == "1"

    click "#link1"
    alert() == "javascript resource loaded!"
    acceptAlert()
    notDisplayed ".invisibleText"

    click "#link0"
    click (elementWithText "a" "Exit SPA")
    on "/Sample1"
  
start firefox
run()
quit()
