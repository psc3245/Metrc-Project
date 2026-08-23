# Entity Plan

## User
* userId - Guid
* username - str
* passwordHash - hash (str)
* projects[]
* tickets[] assignments
* createdAt - datetime

## Project
* projectId - guid
* name - string
* deadline - datetime?
* user[] participants
* ticket[] tickets
* createdAt - datetime


## Ticket
* ticketId - guid
* project
* title - string
* description - string
* priority - enum (LOW | MED | HIGH)
* status - enum (TO DO | IN PROGRESS | IN REVIEW | DONE | other?)
* tag[] - Tag[]
* user? assignee
* createdAt - datetime
* updatedAt - datetime?
* comments[]

## Comment
* commentId
* commenterId
* ticketId
* text - str
* createdAt - datetime