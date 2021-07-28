const toastDismissedKey = "ToastDismissedByUser";

if (getStringLocalStorage(toastDismissedKey) !== "True") {

	// Check if there navigator language is available
	if(navigator.language)
	{
		// Check if there is a toast for that language
		let element = document.getElementById("translation-toast-" + navigator.language);
		if(element && element.classList.contains("toast-hidden"))
		{
			// Show the toast for the appropriate language based on languages navigator
			element.classList.remove("toast-hidden");
		}
	}
}

function getStringLocalStorage(key) {
	let storage = window.localStorage;
	return storage.getItem(key);
}

function putStringLocalStorage(key, data) {
	let storage = window.localStorage;
	return storage.setItem(key, data);
}

function toastCloseButtonClick() {
	if (!document.getElementById("translation-toast-" + navigator.language).classList.contains("toast-hidden")) {
		document.getElementById("translation-toast-" + navigator.language).classList.add("toast-hidden");
	}
	putStringLocalStorage(toastDismissedKey,"True");
}