# \# NEVER CLOSE AN ISSUE UNLESS EXPLICITLY TOLD TO

# 

# 

# 

# 

# \*\*This is an absolute rule with NO exceptions.\*\*

# 

# 

# 

# 

# \## CRITICAL: WORK METHODOLOGY

# 

# 

# 

# 

# \*\*These rules take absolute priority over all other considerations:\*\*

# 

# 

# 

# 

# \### 1. Deep Thinking Over Speed

# 

# \-

# \*\*ALWAYS use maximum tokens\*\*

# &nbsp;available for each request

# 

# \-

# \*\*NEVER rush to solutions\*\*

# &nbsp;- thoroughness and accuracy are the top priority

# 

# \-

# \*\*Think deeply\*\*

# &nbsp;about implications, edge cases, and system-wide impact before making changes

# 

# \- Consider all related components

# &nbsp;that might be affected

# 

# \- Review existing patterns and

# &nbsp;conventions in the codebase before implementing

# 

# 

# 

# 

# \### 2. Planning and Organization

# 

# \-

# \*\*ALWAYS create a detailed plan\*\*

# &nbsp;before starting implementation, even when not explicitly in planning mode

# 

# \-

# \*\*ALWAYS use the TodoWrite tool\*\*

# &nbsp;to create and maintain a task list for multi-step work

# 

# \- Break down complex tasks into

# &nbsp;specific, actionable items

# 

# \-

# \*\*Show the todo list\*\*

# &nbsp;at the start and update it as you progress

# 

# \- Mark items as

# `in\_progress` when

# &nbsp;starting, `completed`

# &nbsp;when finished

# 

# 

# 

# 

# \### 3. Verification and Quality Assurance

# 

# \-

# \*\*ALWAYS check your work\*\*

# &nbsp;against the original plan when completed

# 

# \- Verify that all requirements

# &nbsp;have been met

# 

# \- Review all changes for consistency

# &nbsp;with codebase patterns

# 

# \- Test critical paths mentally

# &nbsp;before declaring work complete

# 

# \- Look for potential issues

# &nbsp;introduced by the changes

# 

# 

# 

# 

# \### 4. Communication and Transparency

# 

# \-

# \*\*ALWAYS provide maximum feedback\*\*

# &nbsp;as you work through tasks

# 

# \- Explain what you're doing

# &nbsp;and why at each step

# 

# \- Share your reasoning for implementation

# &nbsp;decisions

# 

# \- Call out any assumptions or

# &nbsp;uncertainties

# 

# \- Describe what you're looking

# &nbsp;for when searching/reading code

# 

# \-

# \*\*Keep the todo list visible\*\*

# &nbsp;and updated throughout the conversation

# 

# 

# 

# 

# \### 5. No Silent Work

# 

# \- NEVER make changes without

# &nbsp;explaining them

# 

# \- NEVER skip showing intermediate

# &nbsp;steps

# 

# \- NEVER assume the user knows

# &nbsp;what you're thinking

# 

# \-

# \*\*Always narrate your process\*\*

# &nbsp;so the user can follow along

# 

# 

# 

# 

# \*\*Example of good practice:\*\*

# 

# ```

# 

# I'm going to implement the multi-recipient cost calculation. Let me start by creating a plan:

# 

# 

# 

# 

# \[Creates detailed todo list with TodoWrite]

# 

# 

# 

# 

# Now I'll begin with the first item - updating the backend CreditsHelper...

# 

# 

# 

# 

# \[Reads relevant files]

# 

# 

# 

# 

# I can see the current implementation calculates costs without considering recipients.

# 

# I need to add a recipientCount parameter here...

# 

# 

# 

# 

# \[Makes change and explains it]

# 

# 

# 

# 

# Marking this todo as completed. Moving to the next item...

# 

# 

# 

# 

# \[Continues with clear updates throughout]

# 

# 

# 

# 

# All tasks complete! Let me verify against the original plan...

# 

# \[Reviews each item]

# 

# ```

# 

# 

# 

# 

# \### 6. Skills Documentation

# 

# \*\*CRITICAL: When creating or modifying skills in

# `.claude/skills/`,

# &nbsp;you MUST follow the best practices documented in:\*\*

# 

# https://danielmiessler.com/blog/when-to-use-skills-vs-commands-vs-agents

# 

# 

# 

# 

# Key requirements:

# 

# \-

# \*\*TitleCase naming\*\*

# &nbsp;for directories (e.g., `SureDropDatabase`,

# &nbsp;not `suredrop-database`)

# 

# \-

# \*\*YAML frontmatter\*\*

# &nbsp;with `name:` and

# `description:` containing

# &nbsp;trigger keywords

# 

# \-

# \*\*Context files\*\*

# &nbsp;at root level for reference documentation

# 

# \-

# \*\*Workflows/\*\*

# &nbsp;subdirectory for step-by-step procedures

# 

# \-

# \*\*Maximum 2 levels deep\*\*

# &nbsp;(flat hierarchy)

# 

# \-

# \*\*Main SKILL.md\*\*

# &nbsp;acts as routing hub with tables linking to context files and workflows

# 

# 

# 

# 

# 

