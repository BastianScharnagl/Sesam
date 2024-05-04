import face_recognition
import cv2
import numpy as np
import dlib
import paho.mqtt.client as mqtt
from blink_detection import f_blink_detection
import datetime

from FreshestFrame import FreshestFrame
import time
import os
import logging
import serial

logging.basicConfig(level=logging.INFO)

delay=3

mqtt_client = "YOUR CLIENT"
mqtt_username = "YOUR USERNAME"
mqtt_password = "YOUR PASSWORD"
mqtt_broker = "YOUR BROKER"

video_url = 0 #"rtsp://username:password@ip-address/stream1"

def on_disconnect(client, userdata, rc):
    print(rc)

logging.info(f"Connecting Client {mqtt_client} to Broker {mqtt_broker}")
client = mqtt.Client(mqtt_client)
client.username_pw_set(mqtt_username, mqtt_password)
client.on_disconnect = on_disconnect
client.connect(mqtt_broker)


logging.info(f"Opening Video Capture {video_url}")
video_capture = cv2.VideoCapture(video_url)
video_capture.set(cv2.CAP_PROP_FPS, 30)
fresh = FreshestFrame(video_capture)

blink_detector = f_blink_detection.eye_blink_detector()
frontal_face_detector = dlib.get_frontal_face_detector()


logging.info("Load images")
known_face_encodings = []
known_face_names = []

faces_dir = "faces"
faces = os.listdir(faces_dir)
for face in faces:
    logging.info(f"Load image {face}")
    image = face_recognition.load_image_file(os.path.join(faces_dir, face))
    encoding = face_recognition.face_encodings(image)[0]
    name = face.split(".")[0]
    known_face_encodings.append(encoding)
    known_face_names.append(name)

face_locations = []
face_encodings = []
face_names = []
process_this_frame = True

openTime = datetime.datetime.now()
while True:
    try:
        cnt,frame = fresh.read()

        rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)

        face_locations = face_recognition.face_locations(rgb_frame)
        face_encodings = face_recognition.face_encodings(rgb_frame, face_locations)

        for (top, right, bottom, left), face_encoding in zip(face_locations, face_encodings):
            matches = face_recognition.compare_faces(known_face_encodings, face_encoding)

            name = "Unknown"

            face_distances = face_recognition.face_distance(known_face_encodings, face_encoding)
            best_match_index = np.argmin(face_distances)
            if matches[best_match_index]:
                name = known_face_names[best_match_index]
                gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
                rectangles = dlib.rectangle(left, top, right, bottom)
                counter, total = blink_detector.eye_blink(gray, rectangles, 0, 0)
                if counter > 0 and (datetime.datetime.now()-openTime).total_seconds()>delay:
                    logging.info(f"{name} detected: opening door")
                    ret = client.publish("/home/door", "open")
                    logging.info(ret.rc)
                    if ret.rc != 0:
                        logging.info(ret)
                        client.publish("/home/door", "open")
                    openTime = datetime.datetime.now()
    except Exception as e:
        print(e)
    client.loop()
video_capture.release()
cv2.destroyAllWindows()
