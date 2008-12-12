require 'components/gridobject'
require 'serialize/hash'
require 'serialize/array'

module Cpg::Components
	class SimulatedObject < GridObject
		type_register
		
		signal_new('integrated_changed',
			GLib::Signal::RUN_LAST,
			nil,
			nil,
			String)

		property :integrate, :allocation
		read_only :integrate
		invisible :integrate, :allocation
	
		def initialize
			super
		
			self.integrate = Cpg::Serialize::Array.new
			self.allocation = @allocation
		end
	
		def simulation_update(s)
			c = Cpg::MathContext.new(state)

			s.each do |prop, val|
				v = (integrated?(prop) ? c.eval(get_property(prop)).to_f : 0) + val
				set_property(prop, v)
			end
		end
	
		def simulation_step(dt)
			mystate = {}
		
			@links.each do |l| 
				s = l.simulation_evaluate
			
				s.each do |k,v|
					mystate[k] = (mystate[k] || 0) + v * (integrated?(k) ? dt : 1)
				end
			end
		
			mystate
		end
	
		def set_integrated(name, value)
			if value
				self.integrate << name unless self.integrate.include?(name)
			else
				self.integrate.delete(name) if self.integrate.include?(name)
			end
			
			signal_emit('integrated_changed', name)
		end
		
		def integrated?(name)
			self.integrate.include?(name)
		end
		
		def signal_do_integrated_changed(prop)
		end
	end
end
