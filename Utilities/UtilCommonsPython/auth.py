import requests

from cryptography.hazmat.primitives.asymmetric.ed25519 import Ed25519PrivateKey
from cryptography.hazmat.primitives.serialization import Encoding, PublicFormat, PrivateFormat, NoEncryption
from google.protobuf import descriptor_pool, symbol_database
import base64, time, uuid

from pyprotos import Certificate_pb2


def authentification(host, user, password, pub):
    print("Authentification...")
    url = host + "user/auth"
    params = {"username": user, "code": str(password), "pub": pub}
    response = requests.post(url, json=params)
    if response.status_code == 200:
        data = response.json()
        return {"user_certificate": data["cert"], "eas_certificate": data["eas"]}
    else:
        print(response.text)
        raise Exception("Failed to authenticate (code " + str(response.status_code) + ").")


def generate_borrow_cert(
        user_cert: Certificate_pb2.Certificate,
        user_signing_private_key_b64: str,
        authorizations: list[str],
        expiration_unix: int,
        subject: str
) -> Certificate_pb2.Certificate:
    """
    Génère un borrow certificate signé par la clé privée de signature de l'utilisateur.
    
    :param user_cert: Le certificat utilisateur (base_certificate)
    :param user_signing_private_key_b64: Clé privée Ed25519 en base64 (correspond à signature_public_key du user_cert)
    :param authorizations: Liste de droits à déléguer (doit être un sous-ensemble de ceux du user_cert)
    :param expiration_unix: Timestamp Unix d'expiration
    :param issuer: Émetteur du borrow cert
    :return: Certificate (borrow)
    """

    # Vérifier que les autorisations sont bien un sous-ensemble
    user_auths = set(user_cert.payload.content.authorization)
    borrow_auths = set(authorizations)
    if not borrow_auths.issubset(user_auths):
        raise ValueError(f"Autorisations invalides : {borrow_auths - user_auths} absentes du user cert")

    # Construire le payload du borrow cert
    header = Certificate_pb2.CertificateHeader(
        version=Certificate_pb2.Version.V1,
        algorithm="Ed25519",
        type=Certificate_pb2.Type.BORROW,
    )

    content = Certificate_pb2.CertificateContent(
        issuer=user_cert.payload.content.subject,
        subject=subject,  # même subject que le user cert
        unique_id=str(uuid.uuid4()),
        expiration=expiration_unix,
        base_certificate=user_cert,  # embarqué dans le borrow cert
    )
    content.authorization.extend(authorizations)

    payload = Certificate_pb2.CertificatePayload(header=header, content=content)

    # Signer le payload sérialisé en binaire
    payload_bytes = payload.SerializeToString()

    private_key_bytes = base64.b64decode(user_signing_private_key_b64)
    private_key = Ed25519PrivateKey.from_private_bytes(private_key_bytes)
    signature_bytes = private_key.sign(payload_bytes)
    signature_b64 = base64.b64encode(signature_bytes).decode()

    return Certificate_pb2.Certificate(
        payload=payload,
        signature=signature_b64,
    )