#!/usr/bin/ruby

require 'cairo'
require 'components/sensor'

surface = Cairo::SVGSurface.new("icon.svg", 22, 22)
ct = Cairo::Context.new(surface)

ct.set_source_rgba(0, 0, 0, 0)
ct.rectangle(0, 0, 22, 22)
ct.fill

ct.line_width = 1 / 22.0
ct.font_size = ct.font_matrix.xx / 22.0

ct.scale(22, 22)
sensor = Cpg::Components::Sensor.new
sensor.draw(ct)
