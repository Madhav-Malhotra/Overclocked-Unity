# AI Assistant Analytics Events

### UI Trigger Backend Event

| SubType                        | Description                                                                                         |
|--------------------------------|-----------------------------------------------------------------------------------------------------|
| `favorite_conversation`        | Marks a conversation as favorite or not. Includes ConversationId, IsFavorite and ConversationTitle. |
| `delete_conversation`          | Deletes a previous conversation. Includes ConversationId and ConversationTitle.                     |
| `rename_conversation`          | Renames a conversation. Includes ConversationId and ConversationTitle.                              |
| `load_conversation`            | Loads a previously conversation. Includes ConversationId and ConversationTitle.                     |
| `cancel_request`               | Cancels a message request. Includes ConversationId.                                                 |
| `edit_code`                    | User edited the run command script.                                                                 |
| `create_new_conversation`      | User started a new conversation.                                                                    |
| `refresh_inspirational_prompt` | User refreshed inspirational prompt.                                                                |

---

### Context Events

| SubType                                    | Description                                                                                  |
|--------------------------------------------|----------------------------------------------------------------------------------------------|
| `expand_context`                           | User expanded the attached context section.                                                                                              |
| `expand_command_logic`                     | User expanded the command logic section.                                                                                                 |
| `ping_attached_context_object_from_flyout` | User pinged a context object from the flyout. Includes MessageId, ConversationId, ContextType and ContextContent.                        |
| `clear_all_attached_context`               | Cleared all attached context items. Includes MessageId and ConversationId.                                                               |
| `remove_single_attached_context`           | Removed a single attached context item. Includes MessageId, ConversationId, ContextType and ContextContent.                              |
| `drag_drop_attached_context`               | Dragged and dropped a context object. Includes MessageId, ConversationId, ContextType, ContextContent and IsSuccessful.                  |
| `drag_drop_image_file_attached_context`    | Dragged and dropped an image file into context. Includes MessageId, ConversationId, ContextContent (filename) and ContextType (file extension). IsSuccessful is always "true". |
| `choose_context_from_flyout`               | User chose a context object from the flyout. Includes MessageId, ConversationId, ContextType and ContextContent.                         |
| `screenshot_attached_context`              | User attached a screenshot via the screenshot button. Includes MessageId, ConversationId, ContextContent (display name) and ContextType ("Image"). |
| `annotation_attached_context`             | User attached an annotated screenshot via the annotation tool. Includes MessageId, ConversationId, ContextContent (display name) and ContextType ("Image"). Fired both when the initial screenshot is captured and when the annotated version is confirmed. |
| `upload_image_attached_context`            | User uploaded an image file via the file picker. Includes MessageId, ConversationId, ContextContent (display name) and ContextType (file extension). |
| `clipboard_image_attached_context`         | User pasted an image from the clipboard. Includes MessageId, ConversationId and ContextType ("Image"). ContextContent is null.            |

---

### Plugin Events

| SubType       | Description                                  |
|---------------|----------------------------------------------|
| `call_plugin` | User invoked a plugin. Includes PluginLabel. |

---

### UI Trigger Local Event

| SubType                                         | Description                                                                                                   |
|-------------------------------------------------|---------------------------------------------------------------------------------------------------------------|
| `open_shortcuts`                                | Opened the shortcuts panel.                                                                                   |
| `execute_run_command`                           | Ran a command from the UI. Includes MessageId, ConversationId and ResponseMessage                             |
| `use_inspirational_prompt`                      | User clicked an inspirational prompt. Includes UsedInspirationalPrompt.                                       |
| `choose_mode_from_shortcut`                     | User chose a shortcut mode. Includes ChosenMode.                                                              |
| `copy_code`                                     | User copied code from a run command response. Includes ConversationId and ResponseMessage.                    |
| `copy_response`                                 | User copied a response message from any command type. Includes ConversationId, MessageId and ResponseMessage. |
| `save_code`                                     | User saved a response message. Includes ResponseMessage.                                                      |
| `open_reference_url`                            | User clicked on a reference URL. Includes Url.                                                                |
| `modify_run_command_preview_with_object_picker` | User clicked on a reference URL. Includes PreviewParameter.                                                   |
| `modify_run_command_preview_value`              | User clicked on a reference URL. Includes PreviewParameter.                                                   |
| `permission_requested`                          | A permission dialog was displayed to the user. Includes ConversationId, FunctionId and PermissionType. Fired when the dialog appears, before the user responds. |
| `permission_response`                           | User responded to a permission request. Includes ConversationId, FunctionId, UserAnswer and PermissionType.   |
| `window_closed`                                 | User closed the AI Assistant window.                                                                          |
| `permission_setting_changed`                    | User changed a permission policy setting in the settings window. PermissionType contains the setting name; UserAnswer contains the new policy value. |
| `auto_run_setting_changed`                      | User toggled the Auto-Run setting. UserAnswer contains the new value ("True" or "False").                     |
| `new_chat_suggestions_shown`                    | The new-conversation screen with suggestion prompts became visible. No extra fields.                          |
| `suggestion_category_selected`                  | User clicked a suggestion category chip. Includes SuggestionCategory (e.g. "Troubleshoot", "Explore").       |
| `suggestion_prompt_selected`                    | User clicked a specific suggestion prompt. Includes SuggestionCategory and UsedInspirationalPrompt (the prompt text). |

---
---

## Field Schema Details

### Common Fields

| Field Name | Type   | Description                                             |
|------------|--------|---------------------------------------------------------|
| `SubType`  | string | Describes the specific type of action within the group. |

---

### UITriggerBackendEventData

| Field Name          | Type   | Description                                           |
|---------------------|--------|-------------------------------------------------------|
| `SubType`           | string | Specific subtype like 'cancel_request', etc.          |
| `ConversationId`    | string | ID of the conversation where the event occurred.      |
| `MessageId`         | string | ID of the message involved in the event.              |
| `ResponseMessage`   | string | The actual message text (if applicable).              |
| `ConversationTitle` | string | Title of the conversation.                            |
| `IsFavorite`        | string | Indicates if the conversation was marked as favorite. |

---

### ContextEventData

| Field Name       | Type   | Description                                     |
|------------------|--------|-------------------------------------------------|
| `SubType`        | string | Subtype like 'drag_drop_attached_context', etc.                                                       |
| `ContextContent` | string | Name or content of the context object. Null for `clipboard_image_attached_context` and `clear_all_attached_context`. |
| `ContextType`    | string | Type of the context object (e.g. type name, "Image", "LogData", file extension). Null for `clear_all_attached_context`. |
| `IsSuccessful`   | string | Whether the context interaction succeeded. Set for `drag_drop_attached_context` and `drag_drop_image_file_attached_context` only. |
| `MessageId`      | string | ID of the pending message this context was attached to.                                               |
| `ConversationId` | string | ID of the conversation this context was attached to.                                                  |

---

### PluginEventData

| Field Name    | Type   | Description                                 |
|---------------|--------|---------------------------------------------|
| `SubType`     | string | Subtype such as 'call_plugin', etc.         |
| `PluginLabel` | string | Identifier or label for the plugin invoked. |

---

### UITriggerLocalEventData

| Field Name                | Type   | Description                                            |
|---------------------------|--------|--------------------------------------------------------|
| `SubType`                 | string | Subtype such as 'open_shortcuts', etc.                 |
| `UsedInspirationalPrompt` | string | Inspirational prompt being used.                       |
| `SuggestionCategory`      | string | The suggestion category chip label (e.g. "Troubleshoot", "Explore"). Set for `suggestion_category_selected` and `suggestion_prompt_selected`. |
| `ChosenMode`              | string | Selected mode, such as run or ask mode.                |
| `ReferenceUrl`            | string | URL of the reference being called.                     |
| `ConversationId`          | string | ID of the conversation where the event occurred.       |
| `MessageId`               | string | ID of the message involved in the event.               |
| `ResponseMessage`         | string | Message content involved in the UI action.             |
| `PreviewParameter`        | string | Run command preview parameter after user modification. |
| `FunctionId`              | string | ID of the function/tool that requested the permission. |
| `UserAnswer`              | string | User's response or new setting value. For `permission_response`: "AllowOnce", "AllowAlways", or "DenyOnce". For `permission_setting_changed`: the new policy value. For `auto_run_setting_changed`: "True" or "False". |
| `PermissionType`          | string | For `permission_requested`/`permission_response`: category of permission (e.g. "ToolExecution", "FileSystem", "CodeExecution", etc.). For `permission_setting_changed`: the name of the setting that changed. |

---

### SendUserMessageEventData

| Field Name       | Type   | Description                                                                                         |
|------------------|--------|-----------------------------------------------------------------------------------------------------|
| `userPrompt`     | string | The user-entered prompt that triggered the event.                                                   |
| `commandMode`    | string | The mode the assistant was in when the message was sent. (Currently not set — reserved for future use.) |
| `conversationId` | string | Associated conversation ID.                                                                         |
| `messageId`      | string | ID of the user message being sent.                                                                  |
