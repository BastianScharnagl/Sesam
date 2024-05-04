#include <ESP8266WiFi.h>
#include <PubSubClient.h>

const int RELAY = 0;

const char* ssid = "YOUR SSID";
const char* password = "YOUR PASSWORD";

const char* mqtt_server = "YOUR SERVER";
const char* mqtt_user = "YOUR USER";
const char* mqtt_password = "YOUR PASSWORD";

char in_message[100];

WiFiClient espClient;
PubSubClient client(espClient);

bool opened = false;
int openingTime = 0;
int openTime = 3000;

void callback(char* topic, byte* payload, unsigned int length) {
  int i=0;
  for (i;i<length;i++) {
    in_message[i]=char(payload[i]);
  }
  in_message[i]='\0';
}

void setup() {
  pinMode(RELAY, OUTPUT);
  pinMode(LED_BUILTIN, OUTPUT);
  digitalWrite(RELAY, HIGH);
  digitalWrite(LED_BUILTIN, HIGH);
  
  WiFi.begin(ssid, password);

  while(WiFi.status() != WL_CONNECTED)
  {
    delay(500);
  }

  client.setServer(mqtt_server, 1883);
  client.setCallback(callback);
}

void loop()
{
  if(!client.connected())
  {
    reconnect();
  }
  if(strcmp(in_message, "open") == 0)
  {
    memset(&in_message[0], 0, sizeof(in_message));
    digitalWrite(LED_BUILTIN, LOW);
    digitalWrite(RELAY, LOW);
    openingTime = millis();
    opened = true;
  }

  if(opened)
  {
    if(millis() - openingTime >= openTime)
    {
      digitalWrite(LED_BUILTIN, HIGH);
      digitalWrite(RELAY, HIGH);
      opened=false;
    }
  }
  client.loop();
}

void reconnect()
{
  if(client.connect("ESP8266-Relay", mqtt_user, mqtt_password, "/home/door",1,true,"offline"))
  {
    client.subscribe("/home/door");
  }
}
