import { AddMessage } from "./Firebase";

const STORAGE_ROOM_CODE = "storage.RoomCode"
// Room code input element reference
let inputRoomCode:HTMLInputElement;
// Query params in which to look for room codes
const QUERY_PARAMS_ROOM_CODE = ['rc', 'roomcode', 'RoomCode'];

export async function ActionA() {
    localStorage.setItem(STORAGE_ROOM_CODE, inputRoomCode.value)
    let newDocId = await AddMessage(inputRoomCode.value, "Hello A!");
    console.log(`Added doc [ ${newDocId} ]`)
}

export async function ActionB() {
    localStorage.setItem(STORAGE_ROOM_CODE, inputRoomCode.value)
    let newDocId = await AddMessage(inputRoomCode.value, "Hello B!");
    console.log(`Added doc [ ${newDocId} ]`)
}

// Bind to HTML elements by ID
export function InitPage() {
    inputRoomCode = document.getElementById("input.RoomCode") as HTMLInputElement
    let buttonA:HTMLButtonElement = document.getElementById("button.ActionA") as HTMLButtonElement
    buttonA.onclick = ActionA
    let buttonB:HTMLButtonElement = document.getElementById("button.ActionB") as HTMLButtonElement
    buttonB.onclick = ActionB

    // See if a room code was declared as part of this URL, or try and fetch the last known code from storage
    const params =new URLSearchParams(window.location.search);
    const paramRoomCodeKey = QUERY_PARAMS_ROOM_CODE.find(p => params.get(p))
    const roomCode = paramRoomCodeKey
        ? params.get(paramRoomCodeKey)
        : localStorage.getItem(STORAGE_ROOM_CODE)
    if (roomCode)
        inputRoomCode.value = roomCode.toLocaleUpperCase();
}

InitPage();