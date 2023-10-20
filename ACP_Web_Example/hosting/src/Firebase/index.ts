import { initializeApp } from 'firebase/app';
import { getFirestore, doc, collection, addDoc } from 'firebase/firestore';

// Firebase config, populated by the .env file
const firebaseConfig = {
    apiKey: process.env.FIREBASE_API_KEY,
    authDomain: process.env.FIREBASE_AUTH_DOMAIN,
    databaseURL: process.env.FIREBASE_DATABASE_URL,
    projectId: process.env.FIREBASE_PROJECT_ID,
    storageBucket: process.env.FIREBASE_STORAGE_BUCKET,
    messagingSenderId: process.env.FIREBASE_MESSAGING_SENDER_ID,
    appId: process.env.FIREBASE_APP_ID,
    measurementId: process.env.FIREBASE_MEASUREMENT_ID
};


const FIRESTORE_COLLECTION = "Messages"
const app = initializeApp(firebaseConfig)
const db = getFirestore(app)

function MakePayloadDoc(roomCode: string, payload:string) {
    return {
        RoomCode: roomCode.toLocaleUpperCase(),
        Payload: payload,
        DateTime: new Date()
    };
}

/**
 *  Append a new message with a automatically generated doc id
 * @returns The document id
 **/
export async function AddMessage(roomCode:string, payload:string): Promise<string> {
    try {
        const data = MakePayloadDoc(roomCode, payload)
        let result = await addDoc(collection(db, FIRESTORE_COLLECTION), data);
        return result.id;
    } catch (err) {
        console.error(err);
    }
    return null;
}