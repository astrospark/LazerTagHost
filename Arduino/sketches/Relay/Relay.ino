#include <IRremote.h>
#include <LazerTagIr.h>

int RECEIVE_PIN = 11;

#define SERIAL_BUFFER_SIZE 64
char serialBuffer[SERIAL_BUFFER_SIZE];
int serialBufferPosition = 0;

LazerTagIrReceive lazerTagReceive(RECEIVE_PIN);
LazerTagIrSend lazerTagSend;
decode_results results;

void setup();
void loop();
void processSignature(decode_results *results);
void readSerial();
void processCommand(char command[]);

void setup()
{
  Serial.begin(115200);
  lazerTagReceive.enableIRIn();
  lazerTagReceive.blink13(true);
}

void loop()
{
  if (lazerTagReceive.decode(&results))
  {
    processSignature(&results);
    lazerTagReceive.resume();
  }
  else
  {
    readSerial();
  }
}

void processSignature(decode_results *results)
{
  if (results->decode_type != TYPE_LAZERTAG_BEACON && results->decode_type != TYPE_LAZERTAG_TAG)
  {
      Serial.print("RAW ");
      for (int i = 0; i < results->rawlen; i++)
      {
        Serial.print(results->rawbuf[i] * USECPERTICK, HEX);
        if (i < (results->rawlen - 1)) Serial.print(" ");
      }
      Serial.println();
      return;
  }
  
  Serial.print("RCV ");
  Serial.print(results->value, HEX);
  Serial.print(" ");
  Serial.print(results->bits, HEX);
  Serial.print(" ");
  Serial.print(results->decode_type == TYPE_LAZERTAG_BEACON ? 1 : 0);
  Serial.println();
}

void readSerial()
{
    while (Serial.available())
    {
      char previousChar;
      if (serialBufferPosition > 0)
      {
        previousChar = serialBuffer[serialBufferPosition - 1];
      }
      
      char thisChar = Serial.read();
      serialBuffer[serialBufferPosition] = thisChar;
      
      if (previousChar == '\r' && thisChar == '\n')
      {
        serialBuffer[serialBufferPosition - 1] = '\0';
        processSerialLine(serialBuffer);
        serialBufferPosition = 0;
      }
      else
      {
        serialBufferPosition++;
        if (serialBufferPosition >= SERIAL_BUFFER_SIZE) serialBufferPosition = 0;
      }
    }
}

void processSerialLine(char line[])
{
  const char *delimiters = " ";
  
  char *token = strtok (line, delimiters);
  if (token == NULL || strcasecmp(token, "cmd") != 0)
  {
    Serial.print("ERROR Invalid command: ");
    Serial.println(token);
    return;
  }
  
  token = strtok (NULL, delimiters);
  if (token == NULL)
  {
    Serial.println("ERROR Missing command number.");
    return;    
  }
  int command = strtol(token, NULL, 16);

  switch (command)
  {
    case 0x10:
    {
      token = strtok (NULL, delimiters);
      if (token == NULL)
      {
        Serial.println("ERROR Missing data parameter.");
        return;    
      }
      short data = strtol(token, NULL, 16);
      
      token = strtok (NULL, delimiters);
      if (token == NULL)
      {
        Serial.println("ERROR Missing bit count parameter.");
        return;    
      }
      int bitCount = strtol(token, NULL, 16);

      token = strtok (NULL, delimiters);
      if (token == NULL)
      {
        Serial.println("ERROR Missing beacon parameter.");
        return;    
      }
      bool isBeacon = (strtol(token, NULL, 16) != 0);
      
      lazerTagSend.sendLazerTag(data, bitCount, isBeacon);

      lazerTagReceive.enableIRIn();
      lazerTagReceive.resume();
  
      break;
    }
    default:
      Serial.print("ERROR Invalid command number: ");
      Serial.println(command, HEX);
  }
}

