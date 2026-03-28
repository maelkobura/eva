import requests

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