import asyncio
import struct
import base64

import websockets
from cryptography.hazmat.primitives.asymmetric.ed25519 import Ed25519PrivateKey

from pyprotos import Handshake_pb2
from pyprotos import Certificate_pb2


def _sign_int(value: int, private_key_base64: str) -> bytes:
    private_key_bytes = base64.b64decode(private_key_base64)
    private_key = Ed25519PrivateKey.from_private_bytes(private_key_bytes)
    # BitConverter.GetBytes(int) -> little-endian 4 bytes
    data = struct.pack("<i", value)
    return private_key.sign(data)


async def _receive_full_message(ws) -> bytes | None:
    message = await ws.recv()
    if isinstance(message, str):
        return message.encode()
    return message


async def handshake(
    url: str,
    name: str,
    certificate_raw_base64: str,
    private_key_base64: str,
    first_connection: bool = False,
    timeout: float = 10.0,
) -> str | None:
    """
    Performs the handshake with an EVA node.

    Args:
        url: node address (e.g. "192.168.1.1:5000")
        name: local name for logging
        certificate_raw_base64: local certificate encoded in base64
        private_key_base64: raw Ed25519 private key encoded in base64
        first_connection: indicates whether this is the first connection
        timeout: global timeout in seconds

    Returns:
        The node_trust_certificate (str) if successful, None otherwise
    """
    uri = f"ws://{url}/handshake"

    # Decode the certificate to extract the subject (name field in init)
    cert_bytes = base64.b64decode(certificate_raw_base64)
    cert = Certificate_pb2.Certificate()
    cert.ParseFromString(cert_bytes)
    subject = cert.payload.content.subject

    try:
        async with asyncio.timeout(timeout):
            async with websockets.connect(uri) as ws:

                # ---- INIT ----
                init_payload = Handshake_pb2.HandshakeInitialization()
                init_payload.name = subject
                init_payload.certificate = certificate_raw_base64
                init_payload.initialization = first_connection

                init_msg = Handshake_pb2.Handshake()
                init_msg.step = Handshake_pb2.HandshakeStep.INITIALIZATION
                init_msg.payload = init_payload.SerializeToString()

                await ws.send(init_msg.SerializeToString())
                print(f"[{name}] Init sent (subject={subject})")

                # ---- RECEIVE CHALLENGE ----
                challenge_bytes = await _receive_full_message(ws)
                if challenge_bytes is None:
                    print(f"[{name}] No challenge response received")
                    return None

                challenge = Handshake_pb2.HandshakeChallenge()
                challenge.ParseFromString(challenge_bytes)

                # ---- SOLVE CHALLENGE ----
                product = challenge.FirstFactor * challenge.SecondFactor
                signature = _sign_int(product, private_key_base64)

                result_payload = Handshake_pb2.HandshakeChallengeResult()
                result_payload.Result = product
                result_payload.Signature = signature

                result_msg = Handshake_pb2.Handshake()
                result_msg.step = Handshake_pb2.HandshakeStep.CHALLENGE_RESULT
                result_msg.payload = result_payload.SerializeToString()

                await ws.send(result_msg.SerializeToString())
                print(f"[{name}] Challenge solved (product={product})")

                # ---- RECEIVE VALIDATION ----
                validation_bytes = await _receive_full_message(ws)
                if validation_bytes is None:
                    print(f"[{name}] No validation response received")
                    return None

                validation = Handshake_pb2.HandshakeValidation()
                validation.ParseFromString(validation_bytes)

                # ---- CLOSE ----
                await ws.close()

                if validation.success:
                    print(f"[{name}] Handshake successful")
                    return validation.node_trust_certificate if validation.HasField("node_trust_certificate") else None
                else:
                    print(f"[{name}] Handshake rejected by server")
                    return None

    except asyncio.TimeoutError:
        print(f"[{name}] Handshake aborted (timeout)")
        return None
    except websockets.exceptions.WebSocketException as e:
        print(f"[{name}] WebSocket error: {e}")
        return None
    except Exception as e:
        print(f"[{name}] {e}")
        return None