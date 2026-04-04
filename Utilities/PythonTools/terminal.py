import asyncio
import base64
import struct

import websockets
from cryptography.hazmat.primitives.asymmetric.ed25519 import Ed25519PrivateKey

from UtilCommonsPython.auth import authentification, generate_borrow_cert
from UtilCommonsPython.handshake import handshake
from UtilCommonsPython.keys import generate_ed25519_keypair_base64
from pyprotos import Terminal_pb2
from pyprotos import Certificate_pb2
from pyprotos import Functions_pb2

import uuid




# ─── Décodage de la réponse ──────────────────────────────────────────────────

def decode_return_value(return_type: Functions_pb2.EvaType, value: bytes) -> str:
    """Convertit la valeur de retour binaire en string lisible."""
    match return_type:
        case Functions_pb2.EvaType.STRING:
            return value.decode("utf-8")
        case Functions_pb2.EvaType.INT:
            return str(struct.unpack("<q", value)[0])
        case Functions_pb2.EvaType.FLOAT:
            return str(struct.unpack("<d", value)[0])
        case Functions_pb2.EvaType.BOOL:
            return "true" if value[0] != 0 else "false"
        case Functions_pb2.EvaType.NULL | Functions_pb2.EvaType.UNDEFINED:
            return "(null)"
        case Functions_pb2.EvaType.OBJECT:
            return value.decode("utf-8")
        case _:
            return f"(raw bytes) {value.hex()}"


# ─── REPL WebSocket ─────────────────────────────────────────────────────────────

async def terminal_repl(
        node: str,
        node_name: str,
        user_cert_b64: str,
        signing_private_key_b64: str,
        node_trust_cert_b64: str,
        tls: bool = False,
):
    scheme = "wss" if tls else "ws"
    url = f"{scheme}://{node}/terms"

    # Désérialiser le user cert depuis le base64 reçu à l'auth
    user_cert = Certificate_pb2.Certificate()
    user_cert.ParseFromString(base64.b64decode(user_cert_b64))

    # Autorisations nécessaires pour le terminal
    TERMINAL_AUTHS = user_cert.payload.content.authorization

    print(f"Connexion à {url}...")

    async with websockets.connect(url, additional_headers={"Authorization": f"Bearer {node_trust_cert_b64}"}) as ws:
        print("Connecté. Tape 'exit' pour quitter.\n")

        while True:
            try:
                command_str = input("eva> ").strip()
            except (EOFError, KeyboardInterrupt):
                print("\nDéconnexion.")
                break

            if command_str.lower() == "exit":
                print("Déconnexion.")
                break

            if not command_str:
                continue

            # Générer un borrow cert frais à chaque commande (TTL court)
            borrow_cert = generate_borrow_cert(
                user_cert=user_cert,
                user_signing_private_key_b64=signing_private_key_b64,
                authorizations=TERMINAL_AUTHS,
                expiration_unix=60,
                subject=user_cert.payload.content.subject,

            )

            # Construire le message protobuf
            command = Terminal_pb2.TerminalCommand(
                command=command_str,
                borrow_certificate=borrow_cert,
            )
            message = Terminal_pb2.TerminalMessage(command=command)

            await ws.send(message.SerializeToString())

            # Attendre la réponse
            raw = await ws.recv()
            response_msg = Terminal_pb2.TerminalMessage()
            response_msg.ParseFromString(raw)

            match response_msg.WhichOneof("payload"):
                case "returns":
                    resp = response_msg.returns
                    result = decode_return_value(resp.return_value.type, resp.value)
                    print(result)
                case "log":
                    print(f"[log] {response_msg.log}")
                case _:
                    print(f"[réponse inattendue] {response_msg}")


# ─── Entrée principale ───────────────────────────────────────────────────────────

if __name__ == "__main__":
    eas_address = "http://localhost:8080/"

    username = input("Username : ")
    code     = input("Code : ")
    nodename     = input("Node name : ")
    node     = input("Node address : ")

    # Génération des clés et authentification
    prv, pub = generate_ed25519_keypair_base64()
    auth = authentification(eas_address, username, code, pub)

    user_cert_b64 = auth["user_certificate"]
    print("\nUser certificate :", user_cert_b64)
    print("EAS certificate  :", auth["eas_certificate"])

    # Handshake avec le nœud
    nt = asyncio.run(handshake(node, username, user_cert_b64, prv, False))
    print("Node trust cert  :", nt)

    # Lancement du REPL
    asyncio.run(terminal_repl(
        node=node,
        node_name=nodename,
        user_cert_b64=user_cert_b64,
        signing_private_key_b64=prv,
        node_trust_cert_b64=nt,
    ))