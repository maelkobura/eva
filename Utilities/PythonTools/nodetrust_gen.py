import asyncio

from NodeTrustGenerator.handshake import handshake
from UtilCommonsPython.auth import authentification
from UtilCommonsPython.keys import generate_ed25519_keypair_base64

eas_address = "http://localhost:8080/"

username = input("Enter username:")
code = input("Enter code:")
node = input("Enter node host (keep empty for skip):")

prv, pub = generate_ed25519_keypair_base64()
auth = authentification(eas_address, username, code, pub)
print()
print("User certificate:", auth["user_certificate"])
print("EAS certificate:", auth["eas_certificate"])

if node != "":
    result = asyncio.run(handshake(node, username, auth["user_certificate"], prv, False))
    print("Node trust certificate:", result)