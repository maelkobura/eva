import base64
from cryptography.hazmat.primitives.asymmetric import ed25519
from cryptography.hazmat.primitives import serialization


def generate_ed25519_keypair_base64():
    """
    Generate an Ed25519 key pair and return both keys as Base64-encoded strings.

    Returns:
        dict: {
            "private_key": <base64 string>,
            "public_key": <base64 string>
        }
    """

    # Generate a new Ed25519 private key
    private_key = ed25519.Ed25519PrivateKey.generate()

    # Derive the corresponding public key
    public_key = private_key.public_key()

    # Export private key as raw bytes (32 bytes for Ed25519)
    private_bytes = private_key.private_bytes(
        encoding=serialization.Encoding.Raw,
        format=serialization.PrivateFormat.Raw,
        encryption_algorithm=serialization.NoEncryption()
    )

    # Export public key as raw bytes (32 bytes for Ed25519)
    public_bytes = public_key.public_bytes(
        encoding=serialization.Encoding.Raw,
        format=serialization.PublicFormat.Raw
    )

    # Encode both keys in Base64 and convert to string
    private_b64 = base64.b64encode(private_bytes).decode("utf-8")
    public_b64 = base64.b64encode(public_bytes).decode("utf-8")

    return private_b64, public_b64