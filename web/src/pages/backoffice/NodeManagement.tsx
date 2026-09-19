import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { BatteryCharging, CalendarClock, MapPin, Plus, Radio, Trash2, Zap } from 'lucide-react'
import { useCreateNode, useNodes } from '../../api/NodesApi'
import type { ApiRequestError } from '../../api/types'
import SolarLoader from '../../components/SolarLoader/SolarLoader'
import { Button } from '../../components/ui/button'
import { Card, CardContent, CardFooter, CardHeader } from '../../components/ui/card'
import { Input } from '../../components/ui/input'
import { Label } from '../../components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../../components/ui/select'
import {
  Sheet,
  SheetBody,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
} from '../../components/ui/sheet'
import { useToast } from '../../lib/useToast'
import type { Node, NodeType, ScheduleWindow } from '../../types/NodeTypes'

const DAYS_OF_WEEK = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'] as const

const EMPTY_SCHEDULE_WINDOW: ScheduleWindow = { dayOfWeek: 'Mon', openTime: '', closeTime: '' }

const EMPTY_FORM = {
  name: '',
  addressLine: '',
  latitude: '',
  longitude: '',
  capacityKwh: '',
  batterySlotCount: '',
  type: 'AC' as NodeType,
  // REQ-NODE 4.1 requires at least one operating window before a node can go live.
  schedule: [EMPTY_SCHEDULE_WINDOW] as ScheduleWindow[],
}

export default function NodeManagement() {
  const { data, isLoading, isError } = useNodes()
  const [isDialogOpen, setDialogOpen] = useState(false)
  const [form, setForm] = useState(EMPTY_FORM)
  const { showToast } = useToast()

  const handleCreateSuccess = (node: Node) => {
    showToast({
      variant: 'success',
      title: 'Node created',
      message: `${node.name} (${node.nodeId}) was added to the grid.`,
    })
    setForm(EMPTY_FORM)
    setDialogOpen(false)
  }

  const handleCreateError = (error: ApiRequestError) => {
    showToast({ variant: 'danger', title: 'Could not create node', message: error.envelope.message })
  }

  const { mutate: createNode, isPending: isCreating } = useCreateNode({
    onSuccess: handleCreateSuccess,
    onError: handleCreateError,
  })

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()

    createNode({
      name: form.name,
      location: {
        latitude: Number(form.latitude) || 0,
        longitude: Number(form.longitude) || 0,
        addressLine: form.addressLine,
      },
      capacityKwh: Number(form.capacityKwh) || 0,
      batterySlotCount: Number(form.batterySlotCount) || 0,
      type: form.type,
      schedule: form.schedule,
    })
  }

  const addScheduleWindow = () => {
    setForm((p) => ({ ...p, schedule: [...p.schedule, { ...EMPTY_SCHEDULE_WINDOW }] }))
  }

  const removeScheduleWindow = (index: number) => {
    setForm((p) => ({ ...p, schedule: p.schedule.filter((_, i) => i !== index) }))
  }

  const updateScheduleWindow = (index: number, field: keyof ScheduleWindow, value: string) => {
    setForm((p) => ({
      ...p,
      schedule: p.schedule.map((window, i) => (i === index ? { ...window, [field]: value } : window)),
    }))
  }

  const nodes = data?.items ?? []

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="sg-h3">Microgrid nodes</h2>
          <p className="sg-body-sm text-sg-text-secondary">
            Solar hubs registered across the grid.
          </p>
        </div>

        <Button onClick={() => setDialogOpen(true)}>
          <Plus size={16} className="mr-2" />
          Add node
        </Button>

        <Sheet open={isDialogOpen} onOpenChange={setDialogOpen}>
          <SheetContent className="p-0">
            <form onSubmit={handleSubmit} className="flex h-full flex-col">
              <SheetHeader>
                <div className="flex items-center gap-3">
                  <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-sg-primary-soft text-sg-primary">
                    <Radio size={18} />
                  </div>
                  <div>
                    <SheetTitle>Add node</SheetTitle>
                    <SheetDescription>Register a new microgrid hub.</SheetDescription>
                  </div>
                </div>
              </SheetHeader>

              <SheetBody className="flex flex-col gap-5">
                <div className="flex flex-col gap-2">
                  <Label htmlFor="name">Name</Label>
                  <Input
                    id="name"
                    value={form.name}
                    onChange={(e) => setForm((p) => ({ ...p, name: e.target.value }))}
                    required
                  />
                </div>
                <div className="flex flex-col gap-2">
                  <Label htmlFor="addressLine">Address</Label>
                  <Input
                    id="addressLine"
                    value={form.addressLine}
                    onChange={(e) => setForm((p) => ({ ...p, addressLine: e.target.value }))}
                    required
                  />
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div className="flex flex-col gap-2">
                    <Label htmlFor="latitude">Latitude</Label>
                    <Input
                      id="latitude"
                      type="number"
                      step="any"
                      min={-90}
                      max={90}
                      value={form.latitude}
                      onChange={(e) => setForm((p) => ({ ...p, latitude: e.target.value }))}
                      required
                    />
                  </div>
                  <div className="flex flex-col gap-2">
                    <Label htmlFor="longitude">Longitude</Label>
                    <Input
                      id="longitude"
                      type="number"
                      step="any"
                      min={-180}
                      max={180}
                      value={form.longitude}
                      onChange={(e) => setForm((p) => ({ ...p, longitude: e.target.value }))}
                      required
                    />
                  </div>
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div className="flex flex-col gap-2">
                    <Label htmlFor="capacityKwh">Capacity (kWh)</Label>
                    <Input
                      id="capacityKwh"
                      type="number"
                      value={form.capacityKwh}
                      onChange={(e) => setForm((p) => ({ ...p, capacityKwh: e.target.value }))}
                      required
                    />
                  </div>
                  <div className="flex flex-col gap-2">
                    <Label htmlFor="batterySlotCount">Battery slots</Label>
                    <Input
                      id="batterySlotCount"
                      type="number"
                      value={form.batterySlotCount}
                      onChange={(e) => setForm((p) => ({ ...p, batterySlotCount: e.target.value }))}
                      required
                    />
                  </div>
                </div>

                <div className="flex flex-col gap-2">
                  <Label htmlFor="type">Type</Label>
                  <Select value={form.type} onValueChange={(value: NodeType) => setForm((p) => ({ ...p, type: value }))}>
                    <SelectTrigger id="type">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="AC">AC</SelectItem>
                      <SelectItem value="DC">DC</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                <div className="flex flex-col gap-3">
                  <div className="flex items-center justify-between">
                    <Label>Weekly schedule</Label>
                    <Button type="button" variant="outline" size="sm" onClick={addScheduleWindow}>
                      <Plus size={14} className="mr-1" />
                      Add window
                    </Button>
                  </div>

                  {form.schedule.map((window, index) => (
                    <div key={index} className="flex items-end gap-2 rounded-md border border-sg-border p-3">
                      <div className="flex flex-1 flex-col gap-1.5">
                        <Label htmlFor={`schedule-day-${index}`} className="sg-caption">
                          Day
                        </Label>
                        <Select
                          value={window.dayOfWeek}
                          onValueChange={(value) => updateScheduleWindow(index, 'dayOfWeek', value)}
                        >
                          <SelectTrigger id={`schedule-day-${index}`}>
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent>
                            {DAYS_OF_WEEK.map((day) => (
                              <SelectItem key={day} value={day}>
                                {day}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      </div>
                      <div className="flex flex-1 flex-col gap-1.5">
                        <Label htmlFor={`schedule-open-${index}`} className="sg-caption">
                          Open
                        </Label>
                        <Input
                          id={`schedule-open-${index}`}
                          type="time"
                          value={window.openTime}
                          onChange={(e) => updateScheduleWindow(index, 'openTime', e.target.value)}
                          required
                        />
                      </div>
                      <div className="flex flex-1 flex-col gap-1.5">
                        <Label htmlFor={`schedule-close-${index}`} className="sg-caption">
                          Close
                        </Label>
                        <Input
                          id={`schedule-close-${index}`}
                          type="time"
                          value={window.closeTime}
                          onChange={(e) => updateScheduleWindow(index, 'closeTime', e.target.value)}
                          required
                        />
                      </div>
                      <Button
                        type="button"
                        variant="outline"
                        size="icon"
                        onClick={() => removeScheduleWindow(index)}
                        disabled={form.schedule.length === 1}
                        aria-label="Remove window"
                      >
                        <Trash2 size={14} />
                      </Button>
                    </div>
                  ))}
                </div>
              </SheetBody>

              <SheetFooter>
                <Button type="button" variant="outline" onClick={() => setDialogOpen(false)}>
                  Cancel
                </Button>
                <Button type="submit" disabled={isCreating}>
                  {isCreating ? 'Creating...' : 'Create node'}
                </Button>
              </SheetFooter>
            </form>
          </SheetContent>
        </Sheet>
      </div>

      {isLoading ? (
        <div className="flex justify-center py-16">
          <SolarLoader showLabel label="Loading nodes..." />
        </div>
      ) : isError ? (
        <div className="sg-alert sg-alert-danger" role="alert">
          Could not load nodes. Please try again.
        </div>
      ) : nodes.length === 0 ? (
        <div className="flex flex-col items-center justify-center gap-3 rounded-lg border border-dashed border-sg-border py-16 text-center">
          <Zap size={28} className="text-sg-text-muted" />
          <p className="sg-h4">No nodes yet</p>
          <p className="sg-body-sm text-sg-text-secondary">Registered microgrid hubs will appear here.</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {nodes.map((node) => (
            <Card key={node.nodeId} className="flex flex-col overflow-hidden">
              <CardHeader className="flex-row items-start justify-between gap-3 space-y-0 border-b border-sg-border bg-sg-surface-muted/60 pb-4">
                <div className="flex items-start gap-3">
                  <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-sg-primary-soft text-sg-primary">
                    <Radio size={18} />
                  </div>
                  <div className="min-w-0">
                    <p className="sg-h4 truncate">{node.name}</p>
                    <p className="sg-caption font-mono">{node.nodeId}</p>
                  </div>
                </div>
                <span className={`sg-badge shrink-0 ${node.isActive ? 'sg-badge-success' : 'sg-badge-neutral'}`}>
                  <span className="sg-status-dot" />
                  {node.isActive ? 'Active' : 'Inactive'}
                </span>
              </CardHeader>

              <CardContent className="flex flex-1 flex-col gap-4 pt-4">
                <div className="flex items-start gap-2">
                  <MapPin size={15} className="mt-0.5 shrink-0 text-sg-text-muted" />
                  <p className="sg-body-sm">{node.location.addressLine}</p>
                </div>

                <div className="grid grid-cols-2 gap-3">
                  <div className="rounded-md border border-sg-border p-3">
                    <div className="flex items-center gap-1.5 text-sg-text-muted">
                      <Zap size={13} />
                      <p className="sg-caption">Capacity</p>
                    </div>
                    <p className="mt-1 text-lg font-semibold text-sg-text">
                      {node.capacityKwh}
                      <span className="ml-1 text-xs font-normal text-sg-text-muted">kWh</span>
                    </p>
                  </div>
                  <div className="rounded-md border border-sg-border p-3">
                    <div className="flex items-center gap-1.5 text-sg-text-muted">
                      <BatteryCharging size={13} />
                      <p className="sg-caption">Battery slots</p>
                    </div>
                    <p className="mt-1 text-lg font-semibold text-sg-text">{node.batterySlotCount}</p>
                  </div>
                </div>

                <span className={`sg-badge w-fit ${node.type === 'AC' ? 'sg-badge-info' : 'sg-badge-primary'}`}>
                  {node.type} node
                </span>
              </CardContent>

              <CardFooter className="border-t border-sg-border pt-4">
                <Link to={`/backoffice/nodes/${node.nodeId}/slots`} className="sg-btn sg-btn-ghost sg-btn-sm w-full">
                  <CalendarClock size={14} />
                  View slots
                </Link>
              </CardFooter>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}
